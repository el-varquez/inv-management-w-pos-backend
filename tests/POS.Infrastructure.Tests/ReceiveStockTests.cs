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

public class ReceiveStockTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _ctx;
    private readonly ItemRepository _items;
    private readonly StockMovementRepository _movements;
    private readonly UnitOfWork _uow;
    private readonly FakeCurrentUser _user = new();
    private readonly Guid _categoryId = Guid.NewGuid();

    public ReceiveStockTests()
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
        _uow = new UnitOfWork(_ctx);

        _ctx.Categories.Add(new Category { Id = _categoryId, Name = "General" });
        _ctx.SaveChanges();
    }

    private ReceiveStockCommandHandler Handler()
        => new(_items, _movements, _uow, _user);

    private async Task<Item> SeedAsync(
        string name, int stock = 0, decimal cost = 10m, decimal price = 15m,
        bool isComposite = false)
    {
        var item = new Item
        {
            Name = name,
            Stock = stock,
            CostPrice = cost,
            SellingPrice = price,
            CategoryId = _categoryId,
            IsComposite = isComposite
        };
        await _items.AddAsync(item);
        await _uow.SaveChangesAsync();
        return item;
    }

    [Fact]
    public async Task Receives_lines_updates_prices_and_writes_movements()
    {
        var kopiko = await SeedAsync("Kopiko Blanca", stock: 3);
        var lucky = await SeedAsync("Lucky Me Pancit Canton", stock: 10);

        var command = new ReceiveStockCommand(
            "Aling Rosa Wholesale",
            "Tuesday delivery",
            new List<ReceiveStockLine>
            {
                new(kopiko.Id, 24, 9.50m, 12m),
                new(lucky.Id, 60, 14m, 18m)
            });

        var count = await Handler().Handle(command, CancellationToken.None);

        Assert.Equal(2, count);

        _ctx.ChangeTracker.Clear();
        var k = await _ctx.Items.FindAsync(kopiko.Id);
        Assert.Equal(27, k!.Stock);
        Assert.Equal(9.50m, k.CostPrice);
        Assert.Equal(12m, k.SellingPrice);

        var moves = await _ctx.StockMovements.ToListAsync();
        Assert.Equal(2, moves.Count);
        Assert.All(moves, m =>
        {
            Assert.Equal(StockMovementType.AddStock, m.Type);
            Assert.Equal("Aling Rosa Wholesale", m.SupplierName);
            Assert.Equal("Tuesday delivery", m.Notes);
            Assert.Equal(_user.Id, m.CreatedBy);
        });
        Assert.Contains(moves, m => m.ItemId == kopiko.Id && m.Quantity == 24 && m.CostPerUnit == 9.50m);
        Assert.Contains(moves, m => m.ItemId == lucky.Id && m.Quantity == 60 && m.CostPerUnit == 14m);
    }

    [Fact]
    public async Task Blank_supplier_and_notes_normalize_to_null()
    {
        var item = await SeedAsync("Eggs per pc");

        await Handler().Handle(
            new ReceiveStockCommand("  ", "", new List<ReceiveStockLine>
            {
                new(item.Id, 30, 7m, 8.50m)
            }),
            CancellationToken.None);

        _ctx.ChangeTracker.Clear();
        var move = await _ctx.StockMovements.SingleAsync();
        Assert.Null(move.SupplierName);
        Assert.Null(move.Notes);
    }

    [Fact]
    public async Task Composite_line_rejects_the_whole_batch()
    {
        var normal = await SeedAsync("Coffee Beans", stock: 5);
        var composite = await SeedAsync("Creamy Coffee", isComposite: true);

        var command = new ReceiveStockCommand(null, null, new List<ReceiveStockLine>
        {
            new(normal.Id, 10, 8m, 12m),
            new(composite.Id, 1, 20m, 35m)
        });

        await Assert.ThrowsAsync<DomainException>(() =>
            Handler().Handle(command, CancellationToken.None));

        _ctx.ChangeTracker.Clear();
        var reloaded = await _ctx.Items.FindAsync(normal.Id);
        Assert.Equal(5, reloaded!.Stock);                       // untouched
        Assert.Empty(await _ctx.StockMovements.ToListAsync());  // nothing persisted
    }

    [Fact]
    public async Task Unknown_item_throws_not_found()
    {
        var command = new ReceiveStockCommand(null, null, new List<ReceiveStockLine>
        {
            new(Guid.NewGuid(), 1, 1m, 2m)
        });

        await Assert.ThrowsAsync<NotFoundException>(() =>
            Handler().Handle(command, CancellationToken.None));
    }

    [Fact]
    public void Validator_rejects_bad_batches()
    {
        var validator = new ReceiveStockCommandValidator();
        var itemId = Guid.NewGuid();

        Assert.False(validator.Validate(
            new ReceiveStockCommand(null, null, new List<ReceiveStockLine>())).IsValid);

        Assert.False(validator.Validate(
            new ReceiveStockCommand(null, null, new List<ReceiveStockLine>
            {
                new(itemId, 0, 1m, 2m)                          // qty < 1
            })).IsValid);

        Assert.False(validator.Validate(
            new ReceiveStockCommand(null, null, new List<ReceiveStockLine>
            {
                new(itemId, 1, 1m, 0m)                          // selling price 0
            })).IsValid);

        Assert.False(validator.Validate(
            new ReceiveStockCommand(null, null, new List<ReceiveStockLine>
            {
                new(itemId, 1, 1m, 2m),
                new(itemId, 2, 1m, 2m)                          // duplicate item
            })).IsValid);

        Assert.True(validator.Validate(
            new ReceiveStockCommand(null, null, new List<ReceiveStockLine>
            {
                new(itemId, 1, 0m, 2m)                          // zero cost is fine
            })).IsValid);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }
}
