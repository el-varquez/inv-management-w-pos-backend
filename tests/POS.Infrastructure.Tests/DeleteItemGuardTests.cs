using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Application.Inventory.Commands.CreateInventoryCount;
using POS.Application.Inventory.Commands.SetCompositeItem;
using POS.Application.Items.Commands.DeleteItem;
using POS.Application.Sales.Commands.CreateSale;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Domain.Exceptions;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;
using POS.Infrastructure.Tests.Fakes;
using Xunit;

namespace POS.Infrastructure.Tests;

public class DeleteItemGuardTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _ctx;
    private readonly ItemRepository _items;
    private readonly CategoryRepository _categories;
    private readonly CompositeItemRepository _composites;
    private readonly StockMovementRepository _movements;
    private readonly SaleRepository _sales;
    private readonly ShiftRepository _shifts;
    private readonly StoreSettingsRepository _settings;
    private readonly UtangRepository _utang;
    private readonly PaymentMethodRepository _paymentMethods;
    private readonly UnitOfWork _uow;
    private readonly FakeCurrentUser _user = new();
    private readonly Guid _categoryId = Guid.NewGuid();
    private int _codeSeq;

    public DeleteItemGuardTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _ctx = new AppDbContext(options);
        _ctx.Database.EnsureCreated();
        PaymentMethodSeeder.Seed(_ctx);

        _items = new ItemRepository(_ctx);
        _categories = new CategoryRepository(_ctx);
        _composites = new CompositeItemRepository(_ctx);
        _movements = new StockMovementRepository(_ctx);
        _sales = new SaleRepository(_ctx);
        _shifts = new ShiftRepository(_ctx);
        _settings = new StoreSettingsRepository(_ctx);
        _utang = new UtangRepository(_ctx);
        _paymentMethods = new PaymentMethodRepository(_ctx);
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

    private async Task SeedOpenShiftAsync()
    {
        _ctx.Shifts.Add(new Shift
        {
            Number = 1,
            Status = ShiftStatus.Open,
            StartingCash = 1000m,
            OpenedAt = DateTime.UtcNow,
            OpenedBy = _user.Id,
            BusinessDay = new BusinessDay
            {
                Number = 1,
                Status = DayStatus.Open,
                OpenedAt = DateTime.UtcNow,
                OpenedBy = _user.Id
            }
        });
        await _ctx.SaveChangesAsync();
    }

    private CreateSaleCommandHandler SaleHandler() =>
        new(_items, _sales, new FakeReceiptNumberGenerator(), _uow, _user, _composites,
            _shifts, _paymentMethods);

    [Fact]
    public async Task Delete_refuses_an_item_with_sales_history()
    {
        await SeedOpenShiftAsync();
        var item = await SeedAsync("Coke 1L", stock: 10);
        await SaleHandler().Handle(
            new CreateSaleCommand(
                new List<CartItemInput> { new(item.Id, 1, 0m) }, 0m, PaymentMethodIds.Cash, 100m),
            CancellationToken.None);
        var handler = new DeleteItemCommandHandler(_items, _uow);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new DeleteItemCommand(item.Id), CancellationToken.None));
        Assert.Equal("\"Coke 1L\" has sales history — deactivate it instead of deleting.", ex.Message);
        Assert.NotNull(await _ctx.Items.AsNoTracking().SingleOrDefaultAsync(i => i.Id == item.Id));
    }

    [Fact]
    public async Task Delete_refuses_a_component_of_a_composite()
    {
        var parent = await SeedAsync("Snack combo");
        var component = await SeedAsync("Piattos", stock: 5);
        await new SetCompositeItemCommandHandler(_items, _composites, _uow).Handle(
            new SetCompositeItemCommand(parent.Id, new List<ComponentInput> { new(component.Id, 1) }),
            CancellationToken.None);
        var handler = new DeleteItemCommandHandler(_items, _uow);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new DeleteItemCommand(component.Id), CancellationToken.None));
        Assert.Equal("\"Piattos\" is a component of a composite item — remove it from the recipe first.", ex.Message);
    }

    [Fact]
    public async Task Delete_refuses_an_item_in_inventory_count_history()
    {
        var item = await SeedAsync("Rice 1kg", stock: 20);
        var counts = new InventoryCountRepository(_ctx);
        await new CreateInventoryCountCommandHandler(counts, _items, _uow, _user).Handle(
            new CreateInventoryCountCommand("Monthly count"), CancellationToken.None);
        var handler = new DeleteItemCommandHandler(_items, _uow);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new DeleteItemCommand(item.Id), CancellationToken.None));
        Assert.Equal("\"Rice 1kg\" appears in inventory count history — deactivate it instead of deleting.", ex.Message);
    }

    [Fact]
    public async Task Delete_removes_a_clean_item()
    {
        var item = await SeedAsync("Typo item", stock: 3);
        var handler = new DeleteItemCommandHandler(_items, _uow);

        await handler.Handle(new DeleteItemCommand(item.Id), CancellationToken.None);

        Assert.Null(await _ctx.Items.AsNoTracking().SingleOrDefaultAsync(i => i.Id == item.Id));
    }

    [Fact]
    public async Task Delete_refuses_an_item_on_an_invoice()
    {
        var item = await SeedAsync("Invoiced", stock: 10);
        var suki = new Suki { Name = "Aling Rosa", CreatedBy = _user.Id };
        var day = new BusinessDay
        {
            Number = 1, Status = DayStatus.Open, OpenedAt = DateTime.UtcNow, OpenedBy = _user.Id
        };
        var shift = new Shift
        {
            Number = 1, Status = ShiftStatus.Open, OpenedAt = DateTime.UtcNow,
            OpenedBy = _user.Id, BusinessDay = day
        };
        _ctx.Sukis.Add(suki);
        _ctx.Shifts.Add(shift);
        _ctx.Invoices.Add(new Invoice
        {
            InvoiceNumber = "INV-TEST-0001",
            SukiId = suki.Id,
            ShiftId = shift.Id,
            CreatedBy = _user.Id,
            Items = { new InvoiceItem { ItemId = item.Id, ItemName = item.Name, Quantity = 1 } }
        });
        await _ctx.SaveChangesAsync();
        var handler = new DeleteItemCommandHandler(_items, _uow);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new DeleteItemCommand(item.Id), CancellationToken.None));
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }
}
