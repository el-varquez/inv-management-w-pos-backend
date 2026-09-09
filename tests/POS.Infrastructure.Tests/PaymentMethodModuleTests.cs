using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common;
using POS.Application.PaymentMethods.Commands.CreatePaymentMethod;
using POS.Application.PaymentMethods.Commands.UpdatePaymentMethod;
using POS.Application.Sales.Commands.CreateSale;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Domain.Exceptions;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;
using POS.Infrastructure.Tests.Fakes;
using Xunit;

namespace POS.Infrastructure.Tests;

public class PaymentMethodModuleTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _ctx;
    private readonly PaymentMethodRepository _methods;
    private readonly ItemRepository _items;
    private readonly CompositeItemRepository _composites;
    private readonly SaleRepository _sales;
    private readonly ShiftRepository _shifts;
    private readonly StoreSettingsRepository _settings;
    private readonly UtangRepository _utang;
    private readonly UnitOfWork _uow;
    private readonly FakeCurrentUser _user = new();
    private readonly Guid _categoryId = Guid.NewGuid();

    public PaymentMethodModuleTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _ctx = new AppDbContext(options);
        _ctx.Database.EnsureCreated();
        _methods = new PaymentMethodRepository(_ctx);
        _items = new ItemRepository(_ctx);
        _composites = new CompositeItemRepository(_ctx);
        _sales = new SaleRepository(_ctx);
        _shifts = new ShiftRepository(_ctx);
        _settings = new StoreSettingsRepository(_ctx);
        _utang = new UtangRepository(_ctx);
        _uow = new UnitOfWork(_ctx);

        _ctx.Categories.Add(new Category { Id = _categoryId, Name = "General" });
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

    private async Task<Shift> SeedOpenShiftAsync()
    {
        var shift = new Shift
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
        };
        _ctx.Shifts.Add(shift);
        await _ctx.SaveChangesAsync();
        return shift;
    }

    private async Task<Suki> SeedSukiAsync(string name = "Aling Rosa")
    {
        var suki = new Suki { Name = name, CreatedBy = _user.Id };
        await _utang.AddSukiAsync(suki);
        await _uow.SaveChangesAsync();
        return suki;
    }

    private CreateSaleCommandHandler SaleHandler() =>
        new(_items, _sales, new FakeReceiptNumberGenerator(), _uow, _user,
            _composites, _shifts, _methods);

    [Fact]
    public void Seeder_creates_cash_gcash_and_maya_idempotently()
    {
        PaymentMethodSeeder.Seed(_ctx);
        PaymentMethodSeeder.Seed(_ctx);
        var methods = _ctx.PaymentMethods.ToList();

        Assert.Equal(3, methods.Count);
        Assert.All(methods, m => Assert.True(m.IsSystem));
        var cash = methods.Single(m => m.Id == PaymentMethodIds.Cash);
        var gcash = methods.Single(m => m.Id == PaymentMethodIds.GCash);
        var maya = methods.Single(m => m.Id == PaymentMethodIds.Maya);
        Assert.Equal("Cash", cash.Name);
        Assert.False(cash.RequiresReference);
        Assert.Equal("GCash", gcash.Name);
        Assert.True(gcash.RequiresReference);
        Assert.Equal("Maya", maya.Name);
        Assert.True(maya.RequiresReference);
    }

    [Fact]
    public void Seeder_skips_a_method_that_already_exists_by_name()
    {
        _ctx.PaymentMethods.Add(new PaymentMethod { Name = "Maya", IsActive = true });
        _ctx.SaveChanges();

        PaymentMethodSeeder.Seed(_ctx);

        Assert.Equal(3, _ctx.PaymentMethods.Count());
        Assert.DoesNotContain(_ctx.PaymentMethods, m => m.Id == PaymentMethodIds.Maya);
    }

    [Fact]
    public async Task Duplicate_name_hits_the_unique_index()
    {
        PaymentMethodSeeder.Seed(_ctx);
        _ctx.PaymentMethods.Add(new PaymentMethod { Name = "Cash" });
        await Assert.ThrowsAsync<DbUpdateException>(() => _ctx.SaveChangesAsync());
    }

    [Fact]
    public async Task Create_returns_dto_and_persists()
    {
        PaymentMethodSeeder.Seed(_ctx);
        var handler = new CreatePaymentMethodCommandHandler(_methods, _uow);
        var dto = await handler.Handle(
            new CreatePaymentMethodCommand("Bank transfer", true), default);
        Assert.Equal("Bank transfer", dto.Name);
        Assert.True(dto.RequiresReference);
        Assert.False(_ctx.PaymentMethods.Single(m => m.Id == dto.Id).IsSystem);
    }

    [Fact]
    public async Task Create_refuses_duplicate_name_case_insensitively()
    {
        PaymentMethodSeeder.Seed(_ctx);
        var handler = new CreatePaymentMethodCommandHandler(_methods, _uow);
        var ex = await Assert.ThrowsAsync<DomainException>(() => handler.Handle(
            new CreatePaymentMethodCommand("cash", false), default));
        Assert.Equal("A payment method named \"cash\" already exists.", ex.Message);
    }

    [Fact]
    public async Task Cash_cannot_be_deactivated()
    {
        PaymentMethodSeeder.Seed(_ctx);
        var handler = new UpdatePaymentMethodCommandHandler(_methods, _uow);
        var ex = await Assert.ThrowsAsync<DomainException>(() => handler.Handle(
            new UpdatePaymentMethodCommand(PaymentMethodIds.Cash, "Cash", false, false), default));
        Assert.Equal("Cash can't be turned off.", ex.Message);
    }

    [Fact]
    public async Task Update_renames_and_toggles()
    {
        PaymentMethodSeeder.Seed(_ctx);
        var handler = new UpdatePaymentMethodCommandHandler(_methods, _uow);
        await handler.Handle(
            new UpdatePaymentMethodCommand(PaymentMethodIds.Maya, "Lista", false, false), default);
        var maya = _ctx.PaymentMethods.Single(m => m.Id == PaymentMethodIds.Maya);
        Assert.Equal("Lista", maya.Name);
        Assert.False(maya.IsActive);
    }

    [Fact]
    public async Task Inactive_method_refuses_new_sales()
    {
        PaymentMethodSeeder.Seed(_ctx);
        await new UpdatePaymentMethodCommandHandler(_methods, _uow).Handle(
            new UpdatePaymentMethodCommand(PaymentMethodIds.GCash, "GCash", true, false),
            default);
        await SeedOpenShiftAsync();
        var item = await SeedItemAsync();

        var ex = await Assert.ThrowsAsync<DomainException>(() => SaleHandler().Handle(
            new CreateSaleCommand(
                [new CartItemInput(item.Id, 1, 0m)], 0m, PaymentMethodIds.GCash, 25m),
            default));

        Assert.Equal(
            "GCash is turned off — turn it on in web admin Settings.", ex.Message);
    }

    [Fact]
    public async Task Unknown_method_is_not_found()
    {
        PaymentMethodSeeder.Seed(_ctx);
        await SeedOpenShiftAsync();
        var item = await SeedItemAsync();

        await Assert.ThrowsAsync<NotFoundException>(() => SaleHandler().Handle(
            new CreateSaleCommand(
                [new CartItemInput(item.Id, 1, 0m)], 0m, Guid.NewGuid(), 25m),
            default));
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }
}
