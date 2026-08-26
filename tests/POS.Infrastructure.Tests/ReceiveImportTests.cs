using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Application.Inventory.Commands.ReceiveStock;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Domain.Exceptions;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;
using POS.Infrastructure.Tests.Fakes;
using Xunit;

namespace POS.Infrastructure.Tests;

public class ReceiveImportTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _ctx;
    private readonly ItemRepository _items;
    private readonly StockMovementRepository _movements;
    private readonly CategoryRepository _categories;
    private readonly UnitOfWork _uow;
    private readonly FakeCurrentUser _user = new();
    private readonly Guid _customCategoryId = Guid.NewGuid();
    private int _codeSeq;

    public ReceiveImportTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _ctx = new AppDbContext(options);
        _ctx.Database.EnsureCreated();

        _items = new ItemRepository(_ctx);
        _movements = new StockMovementRepository(_ctx);
        _categories = new CategoryRepository(_ctx);
        _uow = new UnitOfWork(_ctx);

        CategorySeeder.Seed(_ctx);
        _ctx.Categories.Add(new Category { Id = _customCategoryId, Name = "General" });
        _ctx.SaveChanges();
    }

    private ReceiveStockCommandHandler Handler()
        => new(_items, _movements, _categories, _uow, _user);

    private async Task<Item> SeedAsync(
        string name, string? barcode = null, int stock = 0,
        bool isComposite = false, bool tracksStock = true)
    {
        var item = new Item
        {
            Name = name,
            Barcode = barcode,
            ItemCode = $"T{++_codeSeq:D4}",
            Stock = stock,
            CostPrice = 10m,
            SellingPrice = 15m,
            CategoryId = _customCategoryId,
            IsComposite = isComposite,
            TracksStock = tracksStock
        };
        await _items.AddAsync(item);
        await _uow.SaveChangesAsync();
        return item;
    }

    private static ReceiveStockLine NewLine(
        string name, string? barcode, int qty, decimal cost, decimal price)
        => new(null, qty, cost, price, new NewReceiveItem(name, barcode));

    [Fact]
    public async Task Creates_a_missing_item_under_inventory_item_and_receives_it()
    {
        var count = await Handler().Handle(
            new ReceiveStockCommand("Aling Rosa Wholesale", null, new List<ReceiveStockLine>
            {
                NewLine("Piattos Cheese 40g", "48001234500011", 10, 12.50m, 16m)
            }),
            CancellationToken.None);

        Assert.Equal(1, count);

        _ctx.ChangeTracker.Clear();
        var created = await _ctx.Items.Include(i => i.Category)
            .SingleAsync(i => i.Name == "Piattos Cheese 40g");
        Assert.Equal("48001234500011", created.Barcode);
        Assert.Equal(CategoryNames.InventoryItem, created.Category.Name);
        Assert.True(created.TracksStock);
        Assert.Equal(10, created.Stock);
        Assert.Equal(12.50m, created.CostPrice);
        Assert.Equal(16m, created.SellingPrice);
        Assert.Equal(5, created.LowStockThreshold);
        Assert.Null(created.UtangMarkup);
        Assert.False(string.IsNullOrWhiteSpace(created.ItemCode));

        var move = await _ctx.StockMovements.SingleAsync();
        Assert.Equal(created.Id, move.ItemId);
        Assert.Equal(StockMovementType.AddStock, move.Type);
        Assert.Equal(10, move.Quantity);
        Assert.Equal(12.50m, move.CostPerUnit);
        Assert.Equal("Aling Rosa Wholesale", move.SupplierName);
    }

    [Fact]
    public async Task Generated_item_codes_are_distinct_within_one_delivery()
    {
        var count = await Handler().Handle(
            new ReceiveStockCommand(null, null, new List<ReceiveStockLine>
            {
                NewLine("Sky Flakes Singles", null, 50, 1.60m, 2.50m),
                NewLine("C2 Apple 230ml", null, 24, 10.50m, 13.50m)
            }),
            CancellationToken.None);

        Assert.Equal(2, count);

        _ctx.ChangeTracker.Clear();
        var codes = await _ctx.Items
            .Where(i => i.Name == "Sky Flakes Singles" || i.Name == "C2 Apple 230ml")
            .Select(i => i.ItemCode)
            .ToListAsync();
        Assert.Equal(2, codes.Count);
        Assert.Equal(2, codes.Distinct().Count());
    }

    [Fact]
    public async Task A_new_item_with_a_known_barcode_resolves_to_the_existing_item()
    {
        var kopiko = await SeedAsync("Kopiko Blanca Twin", barcode: "4800888812345", stock: 3);

        await Handler().Handle(
            new ReceiveStockCommand(null, null, new List<ReceiveStockLine>
            {
                NewLine("Kopiko Blanca (supplier spelling)", "4800888812345", 24, 6.75m, 8m)
            }),
            CancellationToken.None);

        _ctx.ChangeTracker.Clear();
        Assert.Equal(1, await _ctx.Items.CountAsync());
        var item = await _ctx.Items.SingleAsync();
        Assert.Equal(kopiko.Id, item.Id);
        Assert.Equal(27, item.Stock);
        Assert.Equal(6.75m, item.CostPrice);
        Assert.Equal(8m, item.SellingPrice);
    }

    [Fact]
    public async Task A_new_item_with_a_known_name_resolves_case_insensitively()
    {
        var ligo = await SeedAsync("Ligo Sardines 155g", stock: 18);

        await Handler().Handle(
            new ReceiveStockCommand(null, null, new List<ReceiveStockLine>
            {
                NewLine("ligo sardines 155G", null, 12, 19m, 24m)
            }),
            CancellationToken.None);

        _ctx.ChangeTracker.Clear();
        Assert.Equal(1, await _ctx.Items.CountAsync());
        Assert.Equal(30, (await _ctx.Items.SingleAsync(i => i.Id == ligo.Id)).Stock);
    }

    [Fact]
    public async Task A_new_item_resolving_to_a_composite_rejects_the_whole_batch()
    {
        await SeedAsync("Coffee bundle 3s", isComposite: true);
        var normal = await SeedAsync("Nescafe 3-in-1", stock: 5);

        var command = new ReceiveStockCommand(null, null, new List<ReceiveStockLine>
        {
            new(normal.Id, 10, 5.60m, 7m),
            NewLine("Coffee bundle 3s", null, 5, 80m, 95m)
        });

        await Assert.ThrowsAsync<DomainException>(() =>
            Handler().Handle(command, CancellationToken.None));

        _ctx.ChangeTracker.Clear();
        Assert.Equal(5, (await _ctx.Items.SingleAsync(i => i.Id == normal.Id)).Stock);
        Assert.Empty(await _ctx.StockMovements.ToListAsync());
    }

    [Fact]
    public async Task A_line_and_a_new_item_resolving_to_the_same_item_reject_the_batch()
    {
        var kopiko = await SeedAsync("Kopiko Blanca Twin", barcode: "4800888812345", stock: 3);

        var command = new ReceiveStockCommand(null, null, new List<ReceiveStockLine>
        {
            new(kopiko.Id, 5, 6.75m, 8m),
            NewLine("Anything", "4800888812345", 24, 6.75m, 8m)
        });

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            Handler().Handle(command, CancellationToken.None));
        Assert.Equal("Each item can appear only once per delivery.", ex.Message);

        _ctx.ChangeTracker.Clear();
        Assert.Equal(3, (await _ctx.Items.SingleAsync(i => i.Id == kopiko.Id)).Stock);
        Assert.Empty(await _ctx.StockMovements.ToListAsync());
    }

    [Fact]
    public async Task A_failing_line_rolls_back_created_items_too()
    {
        var command = new ReceiveStockCommand(null, null, new List<ReceiveStockLine>
        {
            NewLine("Sky Flakes Singles", null, 50, 1.60m, 2.50m),
            new(Guid.NewGuid(), 1, 1m, 2m)
        });

        await Assert.ThrowsAsync<NotFoundException>(() =>
            Handler().Handle(command, CancellationToken.None));

        _ctx.ChangeTracker.Clear();
        Assert.Empty(await _ctx.Items.Where(i => i.Name == "Sky Flakes Singles").ToListAsync());
        Assert.Empty(await _ctx.StockMovements.ToListAsync());
    }

    [Fact]
    public void Validator_enforces_the_line_shape()
    {
        var validator = new ReceiveStockCommandValidator();
        var itemId = Guid.NewGuid();

        Assert.False(validator.Validate(
            new ReceiveStockCommand(null, null, new List<ReceiveStockLine>
            {
                new(null, 1, 1m, 2m)
            })).IsValid);

        Assert.False(validator.Validate(
            new ReceiveStockCommand(null, null, new List<ReceiveStockLine>
            {
                new(itemId, 1, 1m, 2m, new NewReceiveItem("Both", null))
            })).IsValid);

        Assert.False(validator.Validate(
            new ReceiveStockCommand(null, null, new List<ReceiveStockLine>
            {
                NewLine("Same name", null, 1, 1m, 2m),
                NewLine("same NAME", null, 2, 1m, 2m)
            })).IsValid);

        Assert.False(validator.Validate(
            new ReceiveStockCommand(null, null, new List<ReceiveStockLine>
            {
                NewLine("A", "123", 1, 1m, 2m),
                NewLine("B", "123", 2, 1m, 2m)
            })).IsValid);

        Assert.False(validator.Validate(
            new ReceiveStockCommand(null, null, new List<ReceiveStockLine>
            {
                NewLine("", "123", 1, 1m, 2m)
            })).IsValid);

        Assert.False(validator.Validate(
            new ReceiveStockCommand(null, null, new List<ReceiveStockLine>
            {
                new(null, 1, 1m, 2m, new NewReceiveItem(null!, null))
            })).IsValid);

        Assert.False(validator.Validate(
            new ReceiveStockCommand(null, null, new List<ReceiveStockLine>
            {
                NewLine("No price", null, 1, 1m, 0m)
            })).IsValid);

        Assert.True(validator.Validate(
            new ReceiveStockCommand(null, null, new List<ReceiveStockLine>
            {
                new(itemId, 1, 0m, 2m),
                NewLine("Piattos Cheese 40g", null, 10, 12.50m, 16m)
            })).IsValid);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }
}
