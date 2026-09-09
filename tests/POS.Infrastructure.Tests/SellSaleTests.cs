using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Application.Sales.Commands.CreateSale;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Domain.Exceptions;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;
using POS.Infrastructure.Services;
using POS.Infrastructure.Tests.Fakes;
using Xunit;

namespace POS.Infrastructure.Tests;

public class SellSaleTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _ctx;
    private readonly ItemRepository _items;
    private readonly CompositeItemRepository _composites;
    private readonly SaleRepository _sales;
    private readonly ShiftRepository _shifts;
    private readonly StoreSettingsRepository _settings;
    private readonly UtangRepository _utang;
    private readonly PaymentMethodRepository _paymentMethods;
    private readonly UnitOfWork _uow;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _categoryId = Guid.NewGuid();

    public SellSaleTests()
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
        _composites = new CompositeItemRepository(_ctx);
        _sales = new SaleRepository(_ctx);
        _shifts = new ShiftRepository(_ctx);
        _settings = new StoreSettingsRepository(_ctx);
        _utang = new UtangRepository(_ctx);
        _paymentMethods = new PaymentMethodRepository(_ctx);
        _uow = new UnitOfWork(_ctx);

        _ctx.Categories.Add(new Category { Id = _categoryId, Name = "General" });
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
        _ctx.SaveChanges();
    }

    private async Task<Item> SeedItemAsync(string name = "Coke Mismo 300ml", decimal price = 25m)
    {
        var item = new Item
        {
            Name = name,
            ItemCode = $"X{_ctx.Items.Count() + 1:D4}",
            CostPrice = 10m,
            SellingPrice = price,
            Stock = 50,
            CategoryId = _categoryId
        };
        await _items.AddAsync(item);
        await _uow.SaveChangesAsync();
        return item;
    }

    private CreateSaleCommandHandler Handler(POS.Application.Common.Interfaces.IReceiptNumberGenerator? generator = null) =>
        new(_items, _sales, generator ?? new ReceiptNumberGenerator(_sales), _uow,
            new FakeCurrentUser { Id = _userId, Role = "Cashier" },
            _composites, _shifts, _paymentMethods);

    [Fact]
    public async Task A_gcash_sale_persists_its_reference_number()
    {
        var item = await SeedItemAsync();

        var result = await Handler().Handle(
            new CreateSaleCommand(
                [new CartItemInput(item.Id, 1, 0m)], 0m, PaymentMethodIds.GCash, 25m, "  REF-90210  "),
            CancellationToken.None);

        var saved = await _ctx.Sales.SingleAsync(t => t.Id == result.SaleId);
        Assert.Equal("REF-90210", saved.ReferenceNumber);
    }

    [Fact]
    public async Task A_cash_sale_stores_a_null_reference()
    {
        var item = await SeedItemAsync();

        var result = await Handler().Handle(
            new CreateSaleCommand(
                [new CartItemInput(item.Id, 1, 0m)], 0m, PaymentMethodIds.Cash, 100m),
            CancellationToken.None);

        var saved = await _ctx.Sales.SingleAsync(t => t.Id == result.SaleId);
        Assert.Null(saved.ReferenceNumber);
    }

    [Fact]
    public async Task Receipt_numbers_continue_from_the_days_max_suffix()
    {
        var item = await SeedItemAsync();
        _ctx.Sales.Add(new Sale
        {
            ReceiptNumber = $"R-{DateTime.Now:yyyyMMdd}-0007",
            PaymentMethodId = PaymentMethodIds.Cash,
            CreatedBy = _userId
        });
        await _ctx.SaveChangesAsync();

        var result = await Handler().Handle(
            new CreateSaleCommand(
                [new CartItemInput(item.Id, 1, 0m)], 0m, PaymentMethodIds.Cash, 100m),
            CancellationToken.None);

        Assert.Equal($"R-{DateTime.Now:yyyyMMdd}-0008", result.ReceiptNumber);
    }

    [Fact]
    public async Task Receipt_numbers_ignore_other_days()
    {
        var item = await SeedItemAsync();
        _ctx.Sales.Add(new Sale
        {
            ReceiptNumber = "R-19990101-0099",
            PaymentMethodId = PaymentMethodIds.Cash,
            CreatedBy = _userId
        });
        await _ctx.SaveChangesAsync();

        var result = await Handler().Handle(
            new CreateSaleCommand(
                [new CartItemInput(item.Id, 1, 0m)], 0m, PaymentMethodIds.Cash, 100m),
            CancellationToken.None);

        Assert.Equal($"R-{DateTime.Now:yyyyMMdd}-0001", result.ReceiptNumber);
    }

    [Fact]
    public async Task A_receipt_collision_is_translated_by_the_unit_of_work()
    {
        _ctx.Sales.Add(new Sale
        {
            ReceiptNumber = "R-DUP-0001", PaymentMethodId = PaymentMethodIds.Cash, CreatedBy = _userId
        });
        await _ctx.SaveChangesAsync();
        _ctx.Sales.Add(new Sale
        {
            ReceiptNumber = "R-DUP-0001", PaymentMethodId = PaymentMethodIds.Cash, CreatedBy = _userId
        });

        await Assert.ThrowsAsync<ReceiptNumberCollisionException>(() => _uow.SaveChangesAsync());
    }

    [Fact]
    public async Task A_sale_retries_with_a_fresh_number_after_a_collision()
    {
        var item = await SeedItemAsync();
        _ctx.Sales.Add(new Sale
        {
            ReceiptNumber = "R-DUP-0001", PaymentMethodId = PaymentMethodIds.Cash, CreatedBy = _userId
        });
        await _ctx.SaveChangesAsync();

        var result = await Handler(new FakeCollidingReceiptNumberGenerator("R-DUP-0001", "R-DUP-0002"))
            .Handle(
                new CreateSaleCommand(
                    [new CartItemInput(item.Id, 1, 0m)], 0m, PaymentMethodIds.Cash, 100m),
                CancellationToken.None);

        Assert.Equal("R-DUP-0002", result.ReceiptNumber);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }
}
