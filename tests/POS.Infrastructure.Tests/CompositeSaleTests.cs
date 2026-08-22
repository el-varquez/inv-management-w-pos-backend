using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Application.Inventory.Commands.SetCompositeItem;
using POS.Application.Sales.Commands.CreateTransaction;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Domain.Exceptions;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;
using POS.Infrastructure.Tests.Fakes;
using Xunit;

namespace POS.Infrastructure.Tests;

public class CompositeSaleTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _ctx;
    private readonly ItemRepository _items;
    private readonly CompositeItemRepository _composites;
    private readonly TransactionRepository _transactions;
    private readonly ShiftRepository _shifts;
    private readonly StoreSettingsRepository _settings;
    private readonly UtangRepository _utang;
    private readonly UnitOfWork _uow;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _categoryId = Guid.NewGuid();
    private int _codeSeq;

    public CompositeSaleTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _ctx = new AppDbContext(options);
        _ctx.Database.EnsureCreated();

        _items = new ItemRepository(_ctx);
        _composites = new CompositeItemRepository(_ctx);
        _transactions = new TransactionRepository(_ctx);
        _shifts = new ShiftRepository(_ctx);
        _settings = new StoreSettingsRepository(_ctx);
        _utang = new UtangRepository(_ctx);
        _uow = new UnitOfWork(_ctx);

        _ctx.Categories.Add(new Category { Id = _categoryId, Name = "General" });
        _ctx.SaveChanges();
    }

    private async Task<Item> SeedItemAsync(
        string name, int stock, decimal cost, decimal price = 100, bool isComposite = false)
    {
        var item = new Item
        {
            Name = name,
            ItemCode = $"T{++_codeSeq:D4}",
            CostPrice = cost,
            SellingPrice = price,
            Stock = stock,
            CategoryId = _categoryId,
            IsComposite = isComposite
        };
        await _items.AddAsync(item);
        await _uow.SaveChangesAsync();
        return item;
    }

    private async Task LinkComponentAsync(Guid parentId, Guid componentId, decimal qty)
    {
        await _composites.AddAsync(new CompositeItem
        {
            ParentItemId = parentId,
            ComponentItemId = componentId,
            Quantity = qty
        });
        await _uow.SaveChangesAsync();
    }

    private async Task SeedOpenShiftAsync()
    {
        _ctx.Shifts.Add(new Shift
        {
            Number = 1,
            Status = ShiftStatus.Open,
            StartingCash = 1000m,
            OpenedAt = DateTime.UtcNow,
            OpenedBy = _userId,
            BusinessDay = new BusinessDay
            {
                Number = 1,
                Status = DayStatus.Open,
                OpenedAt = DateTime.UtcNow,
                OpenedBy = _userId
            }
        });
        await _ctx.SaveChangesAsync();
    }

    private CreateTransactionCommandHandler Handler() =>
        new(_items, _transactions, new FakeReceiptNumberGenerator(), _uow,
            new FakeCurrentUser { Id = _userId, Role = "Cashier" },
            _composites, _shifts, _settings, _utang);

    private static CreateTransactionCommand Sale(params (Guid id, int qty)[] lines) =>
        new(
            lines.Select(l => new CartItemInput(l.id, l.qty, 0m)).ToList(),
            0m,
            PaymentType.Cash,
            100000m);

    [Fact]
    public async Task Sale_of_composite_with_insufficient_component_stock_is_rejected()
    {
        await SeedOpenShiftAsync();
        var beans = await SeedItemAsync("Beans", stock: 1, cost: 5m);
        var latte = await SeedItemAsync("Latte", stock: 0, cost: 0m, isComposite: true);
        await LinkComponentAsync(latte.Id, beans.Id, qty: 2m);

        await Assert.ThrowsAsync<InsufficientStockException>(() =>
            Handler().Handle(Sale((latte.Id, 1)), CancellationToken.None));
    }

    [Fact]
    public async Task Aggregate_demand_across_lines_is_rejected_when_total_exceeds_stock()
    {
        await SeedOpenShiftAsync();
        var beans = await SeedItemAsync("Beans", stock: 3, cost: 5m);
        var latte = await SeedItemAsync("Latte", stock: 0, cost: 0m, isComposite: true);
        await LinkComponentAsync(latte.Id, beans.Id, qty: 2m);

        await Assert.ThrowsAsync<InsufficientStockException>(() =>
            Handler().Handle(Sale((latte.Id, 1), (beans.Id, 2)), CancellationToken.None));
    }

    [Fact]
    public async Task Sale_of_composite_with_sufficient_component_stock_succeeds()
    {
        await SeedOpenShiftAsync();
        var beans = await SeedItemAsync("Beans", stock: 10, cost: 5m);
        var latte = await SeedItemAsync("Latte", stock: 0, cost: 0m, isComposite: true);
        await LinkComponentAsync(latte.Id, beans.Id, qty: 2m);

        var result = await Handler().Handle(Sale((latte.Id, 1)), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.TransactionId);
    }

    [Fact]
    public async Task Composite_sale_snapshots_cost_as_sum_of_component_costs()
    {
        await SeedOpenShiftAsync();
        var beans = await SeedItemAsync("Beans", stock: 100, cost: 5m);
        var milk = await SeedItemAsync("Milk", stock: 100, cost: 3m);
        var latte = await SeedItemAsync("Latte", stock: 0, cost: 0m, price: 120m, isComposite: true);
        await LinkComponentAsync(latte.Id, beans.Id, qty: 2m);
        await LinkComponentAsync(latte.Id, milk.Id, qty: 1m);

        var result = await Handler().Handle(Sale((latte.Id, 2)), CancellationToken.None);

        var tx = await _ctx.Transactions
            .Include(t => t.Items)
            .SingleAsync(t => t.Id == result.TransactionId);
        var line = tx.Items.Single();

        Assert.Equal(13m, line.CostPrice);
        Assert.Equal(26m, line.CostPrice * line.Quantity);
    }

    [Fact]
    public void SetComposite_validator_rejects_fractional_component_quantity()
    {
        var cmd = new SetCompositeItemCommand(
            Guid.NewGuid(),
            new List<ComponentInput> { new(Guid.NewGuid(), 0.5m) });

        var result = new SetCompositeItemCommandValidator().Validate(cmd);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void SetComposite_validator_accepts_whole_component_quantity()
    {
        var cmd = new SetCompositeItemCommand(
            Guid.NewGuid(),
            new List<ComponentInput> { new(Guid.NewGuid(), 2m) });

        var result = new SetCompositeItemCommandValidator().Validate(cmd);

        Assert.True(result.IsValid);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }
}
