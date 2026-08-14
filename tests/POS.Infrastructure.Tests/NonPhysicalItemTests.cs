using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Application.Dashboard.Queries.GetDashboardSummary;
using POS.Application.Inventory.Commands.CreateInventoryCount;
using POS.Application.Inventory.Commands.ReceiveStock;
using POS.Application.Inventory.Commands.SetCompositeItem;
using POS.Application.Inventory.Queries.GetLowStockItems;
using POS.Application.Items.Commands.CreateItem;
using POS.Application.Items.Commands.UpdateItem;
using POS.Application.Sales.Commands.CreateTransaction;
using POS.Application.Sales.EventHandlers;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Domain.Events;
using POS.Domain.Exceptions;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;
using POS.Infrastructure.Tests.Fakes;
using Xunit;

namespace POS.Infrastructure.Tests;

public class NonPhysicalItemTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _ctx;
    private readonly ItemRepository _items;
    private readonly CategoryRepository _categories;
    private readonly CompositeItemRepository _composites;
    private readonly StockMovementRepository _movements;
    private readonly TransactionRepository _transactions;
    private readonly UnitOfWork _uow;
    private readonly FakeCurrentUser _user = new();
    private readonly Guid _categoryId = Guid.NewGuid();
    private int _codeSeq;

    public NonPhysicalItemTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _ctx = new AppDbContext(options);
        _ctx.Database.EnsureCreated();

        _items = new ItemRepository(_ctx);
        _categories = new CategoryRepository(_ctx);
        _composites = new CompositeItemRepository(_ctx);
        _movements = new StockMovementRepository(_ctx);
        _transactions = new TransactionRepository(_ctx);
        _uow = new UnitOfWork(_ctx);

        _ctx.Categories.Add(new Category { Id = _categoryId, Name = "General" });
        _ctx.SaveChanges();
    }

    private async Task<Item> SeedAsync(
        string name, bool tracksStock = true, int stock = 0, int threshold = 5)
    {
        var item = new Item
        {
            Name = name,
            ItemCode = $"T{++_codeSeq:D4}",
            Stock = stock,
            LowStockThreshold = threshold,
            CostPrice = 0m,
            SellingPrice = 1m,
            CategoryId = _categoryId,
            TracksStock = tracksStock
        };
        await _items.AddAsync(item);
        await _uow.SaveChangesAsync();
        return item;
    }

    private CreateTransactionCommandHandler SaleHandler() =>
        new(_items, _transactions, new FakeReceiptNumberGenerator(), _uow, _user, _composites);

    [Fact]
    public async Task Items_track_stock_by_default()
    {
        var handler = new CreateItemCommandHandler(_items, _categories, _uow);

        var id = await handler.Handle(
            new CreateItemCommand("Kopiko Blanca", null, null, null, 8m, 10m, 5, _categoryId, null, true),
            CancellationToken.None);

        var stored = await _ctx.Items.AsNoTracking().SingleAsync(i => i.Id == id);
        Assert.True(stored.TracksStock);
    }

    [Fact]
    public async Task Create_persists_a_non_physical_item()
    {
        var handler = new CreateItemCommandHandler(_items, _categories, _uow);

        var id = await handler.Handle(
            new CreateItemCommand("GCash fee", null, null, null, 0m, 1m, 0, _categoryId, null, false),
            CancellationToken.None);

        var stored = await _ctx.Items.AsNoTracking().SingleAsync(i => i.Id == id);
        Assert.False(stored.TracksStock);
    }

    [Fact]
    public async Task Update_can_switch_an_item_to_non_physical()
    {
        var item = await SeedAsync("Photocopy");
        var update = new UpdateItemCommandHandler(_items, _uow);

        await update.Handle(
            new UpdateItemCommand(
                item.Id, "Photocopy", null, null, null, 0m, 2m, 5, _categoryId, true, null, false),
            CancellationToken.None);

        var stored = await _ctx.Items.AsNoTracking().SingleAsync(i => i.Id == item.Id);
        Assert.False(stored.TracksStock);
    }

    [Fact]
    public async Task A_non_physical_item_sells_at_zero_stock()
    {
        var fee = await SeedAsync("GCash fee", tracksStock: false, stock: 0);

        var result = await SaleHandler().Handle(
            new CreateTransactionCommand(
                new List<CartItemInput> { new(fee.Id, 20, 0m) },
                0m,
                PaymentType.Cash,
                20m),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.TransactionId);
    }

    [Fact]
    public async Task Selling_a_non_physical_item_writes_no_stock_movement()
    {
        var fee = await SeedAsync("GCash fee", tracksStock: false, stock: 0);
        var handler = new SaleCompletedEventHandler(_items, _composites, _movements);

        await handler.Handle(
            new SaleCompletedEvent(
                Guid.NewGuid(),
                new List<(Guid, int)> { (fee.Id, 20) },
                Guid.NewGuid()),
            CancellationToken.None);
        await _uow.SaveChangesAsync();

        var stored = await _ctx.Items.AsNoTracking().SingleAsync(i => i.Id == fee.Id);
        Assert.Equal(0, stored.Stock);
        Assert.Empty(await _ctx.StockMovements.AsNoTracking().Where(m => m.ItemId == fee.Id).ToListAsync());
    }

    [Fact]
    public async Task Selling_a_physical_item_still_writes_a_stock_movement()
    {
        var kopiko = await SeedAsync("Kopiko Blanca", stock: 10);
        var handler = new SaleCompletedEventHandler(_items, _composites, _movements);

        await handler.Handle(
            new SaleCompletedEvent(
                Guid.NewGuid(),
                new List<(Guid, int)> { (kopiko.Id, 3) },
                Guid.NewGuid()),
            CancellationToken.None);
        await _uow.SaveChangesAsync();

        var stored = await _ctx.Items.AsNoTracking().SingleAsync(i => i.Id == kopiko.Id);
        Assert.Equal(7, stored.Stock);
        Assert.Single(await _ctx.StockMovements.AsNoTracking().Where(m => m.ItemId == kopiko.Id).ToListAsync());
    }

    [Fact]
    public async Task Voiding_a_non_physical_sale_writes_no_reversing_movement()
    {
        var fee = await SeedAsync("GCash fee", tracksStock: false, stock: 0);
        var handler = new SaleRefundedEventHandler(_items, _composites, _movements);

        await handler.Handle(
            new SaleRefundedEvent(
                Guid.NewGuid(),
                new List<(Guid, int)> { (fee.Id, 20) },
                Guid.NewGuid()),
            CancellationToken.None);
        await _uow.SaveChangesAsync();

        var stored = await _ctx.Items.AsNoTracking().SingleAsync(i => i.Id == fee.Id);
        Assert.Equal(0, stored.Stock);
        Assert.Empty(await _ctx.StockMovements.AsNoTracking().Where(m => m.ItemId == fee.Id).ToListAsync());
    }

    [Fact]
    public async Task Low_stock_query_excludes_non_physical_items()
    {
        await SeedAsync("GCash fee", tracksStock: false, stock: 0, threshold: 5);
        var rice = await SeedAsync("Rice 1kg", stock: 2, threshold: 5);

        var handler = new GetLowStockItemsQueryHandler(_items);
        var result = await handler.Handle(new GetLowStockItemsQuery(), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(rice.Id, result[0].ItemId);
    }

    [Fact]
    public async Task Dashboard_stock_health_ignores_non_physical_items()
    {
        await SeedAsync("GCash fee", tracksStock: false, stock: 0, threshold: 5);
        var rice = await SeedAsync("Rice 1kg", stock: 2, threshold: 5);

        var handler = new GetDashboardSummaryQueryHandler(_transactions, _items);

        var result = await handler.Handle(new GetDashboardSummaryQuery(), CancellationToken.None);

        Assert.Equal(1, result.StockHealth.TotalItems);
        Assert.Equal(0, result.StockHealth.OutOfStock);
        Assert.Single(result.RunningOut);
        Assert.Equal(rice.Id, result.RunningOut[0].ItemId);
    }

    [Fact]
    public async Task Receive_stock_rejects_a_non_physical_line()
    {
        var fee = await SeedAsync("GCash fee", tracksStock: false);
        var handler = new ReceiveStockCommandHandler(_items, _movements, _uow, _user);

        var command = new ReceiveStockCommand(
            null, null,
            new List<ReceiveStockLine> { new(fee.Id, 10, 0m, 1m) });

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Contains("GCash fee", ex.Message);
    }

    [Fact]
    public async Task Composite_rejects_a_non_physical_component()
    {
        var parent = await SeedAsync("Snack combo");
        var fee = await SeedAsync("GCash fee", tracksStock: false);
        var handler = new SetCompositeItemCommandHandler(_items, _composites, _uow);

        var command = new SetCompositeItemCommand(
            parent.Id,
            new List<ComponentInput> { new(fee.Id, 1) });

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Contains("GCash fee", ex.Message);
    }

    [Fact]
    public async Task Inventory_count_excludes_non_physical_items()
    {
        var rice = await SeedAsync("Rice 1kg", stock: 20);
        await SeedAsync("GCash fee", tracksStock: false);

        var counts = new InventoryCountRepository(_ctx);
        var handler = new CreateInventoryCountCommandHandler(counts, _items, _uow, _user);

        var id = await handler.Handle(
            new CreateInventoryCountCommand("Monthly count"), CancellationToken.None);

        var lines = await _ctx.InventoryCountLines.AsNoTracking()
            .Where(l => l.InventoryCountId == id)
            .ToListAsync();

        Assert.Single(lines);
        Assert.Equal(rice.Id, lines[0].ItemId);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }
}
