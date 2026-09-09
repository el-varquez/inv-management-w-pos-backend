using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common;
using POS.Application.PaymentMethods.Commands.CreatePaymentMethod;
using POS.Application.PaymentMethods.Commands.UpdatePaymentMethod;
using POS.Application.Sales.Commands.CreateSale;
using POS.Application.Shifts.Commands.CloseShift;
using POS.Application.Shifts.Commands.CorrectShiftCount;
using POS.Application.Shifts.Commands.OpenShift;
using POS.Application.Shifts.Commands.RecordDrawerMovement;
using POS.Application.Shifts.Commands.UpdateStartingCash;
using POS.Application.Shifts.Commands.VoidDrawerMovement;
using POS.Application.Shifts.Queries.GetCurrentShift;
using POS.Application.Shifts.Queries.GetShiftRead;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Domain.Exceptions;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;
using POS.Infrastructure.Tests.Fakes;
using Xunit;

namespace POS.Infrastructure.Tests;

public class ShiftModuleTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _ctx;
    private readonly ShiftRepository _shifts;
    private readonly BusinessDayRepository _days;
    private readonly ItemRepository _items;
    private readonly SaleRepository _sales;
    private readonly CompositeItemRepository _composites;
    private readonly StoreSettingsRepository _settings;
    private readonly UtangRepository _utang;
    private readonly PaymentMethodRepository _paymentMethods;
    private readonly UnitOfWork _uow;
    private readonly FakeCurrentUser _user = new();
    private readonly Guid _categoryId = Guid.NewGuid();

    public ShiftModuleTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _ctx = new AppDbContext(options);
        _ctx.Database.EnsureCreated();
        PaymentMethodSeeder.Seed(_ctx);

        _shifts = new ShiftRepository(_ctx);
        _days = new BusinessDayRepository(_ctx);
        _items = new ItemRepository(_ctx);
        _sales = new SaleRepository(_ctx);
        _composites = new CompositeItemRepository(_ctx);
        _settings = new StoreSettingsRepository(_ctx);
        _utang = new UtangRepository(_ctx);
        _paymentMethods = new PaymentMethodRepository(_ctx);
        _uow = new UnitOfWork(_ctx);

        _ctx.Categories.Add(new Category { Id = _categoryId, Name = "General" });
        _ctx.SaveChanges();
    }

    private OpenShiftCommandHandler OpenHandler()
        => new(_shifts, _days, _uow, _user);

    private async Task<BusinessDay> SeedOpenDayAsync()
    {
        var day = new BusinessDay
        {
            Number = 1,
            Status = DayStatus.Open,
            OpenedAt = DateTime.UtcNow.AddHours(-8),
            OpenedBy = _user.Id
        };
        await _days.AddAsync(day);
        await _uow.SaveChangesAsync();
        return day;
    }

    private CloseShiftCommandHandler CloseHandler()
        => new(_shifts, _sales, _uow, _user, _paymentMethods);

    private CreateSaleCommandHandler SaleHandler()
        => new(_items, _sales, new FakeReceiptNumberGenerator(), _uow, _user,
            _composites, _shifts, _paymentMethods);

    private static CreateSaleCommand SaleOf(Item item, int qty)
        => new(
            new List<CartItemInput> { new(item.Id, qty, 0m) },
            0m,
            PaymentMethodIds.Cash,
            item.SellingPrice * qty);

    private async Task<Item> SeedItemAsync(string name, int stock = 100, decimal price = 10m)
    {
        var item = new Item
        {
            Name = name,
            ItemCode = $"S{Guid.NewGuid().ToString()[..4]}",
            Stock = stock,
            CostPrice = 5m,
            SellingPrice = price,
            CategoryId = _categoryId
        };
        await _items.AddAsync(item);
        await _uow.SaveChangesAsync();
        return item;
    }

    [Fact]
    public async Task Shift_round_trips_with_its_snapshot()
    {
        var day = await SeedOpenDayAsync();
        var shift = new Shift
        {
            Number = 1,
            Status = ShiftStatus.Closed,
            StartingCash = 2000m,
            OpenedAt = DateTime.UtcNow.AddHours(-8),
            OpenedBy = _user.Id,
            ClosedAt = DateTime.UtcNow,
            ClosedBy = _user.Id,
            BusinessDayId = day.Id,
            Snapshot = new XReadSnapshot
            {
                NetSales = 4320m,
                TransactionCount = 37,
                DrawerMovementsNet = -1350m,
                ExpectedCash = 4130m,
                CountedCash = 4130m,
                CashVariance = 0m
            },
            MethodSales = new List<ShiftMethodSales>
            {
                new() { PaymentMethodId = PaymentMethodIds.Cash, MethodName = "Cash", Amount = 3480m },
                new() { PaymentMethodId = PaymentMethodIds.GCash, MethodName = "GCash", Amount = 840m }
            }
        };

        await _shifts.AddAsync(shift);
        await _uow.SaveChangesAsync();

        var stored = await _ctx.Shifts
            .Include(s => s.MethodSales)
            .AsNoTracking().SingleAsync();
        Assert.Equal(1, stored.Number);
        Assert.Equal(ShiftStatus.Closed, stored.Status);
        Assert.NotNull(stored.Snapshot);
        Assert.Equal(4320m, stored.Snapshot!.NetSales);
        Assert.Equal(-1350m, stored.Snapshot.DrawerMovementsNet);
        Assert.Equal(2, stored.MethodSales.Count);
        Assert.Equal(3480m, stored.MethodSales.Single(m => m.PaymentMethodId == PaymentMethodIds.Cash).Amount);
    }

    [Fact]
    public async Task Opening_a_shift_numbers_it_from_one()
    {
        var id = await OpenHandler().Handle(
            new OpenShiftCommand(2000m), CancellationToken.None);

        var stored = await _ctx.Shifts.AsNoTracking().SingleAsync(s => s.Id == id);
        Assert.Equal(1, stored.Number);
        Assert.Equal(ShiftStatus.Open, stored.Status);
        Assert.Equal(2000m, stored.StartingCash);
        Assert.Equal(_user.Id, stored.OpenedBy);
    }

    [Fact]
    public async Task Opening_while_a_shift_is_open_is_rejected_by_number()
    {
        await OpenHandler().Handle(new OpenShiftCommand(2000m), CancellationToken.None);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => OpenHandler().Handle(new OpenShiftCommand(1500m), CancellationToken.None));

        Assert.Contains("#1", ex.Message);
    }

    [Fact]
    public async Task Selling_with_no_open_shift_is_rejected()
    {
        var item = await SeedItemAsync("Kopiko Blanca");

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => SaleHandler().Handle(SaleOf(item, 1), CancellationToken.None));

        Assert.Contains("No open shift", ex.Message);
    }

    [Fact]
    public async Task A_sale_is_stamped_with_the_open_shift()
    {
        var shiftId = await OpenHandler().Handle(
            new OpenShiftCommand(2000m), CancellationToken.None);
        var item = await SeedItemAsync("Kopiko Blanca");

        var result = await SaleHandler().Handle(SaleOf(item, 2), CancellationToken.None);

        var stored = await _ctx.Sales.AsNoTracking()
            .SingleAsync(t => t.Id == result.SaleId);
        Assert.Equal(shiftId, stored.ShiftId);
    }

    [Fact]
    public async Task A_payout_is_recorded_against_the_open_shift_with_its_note()
    {
        var shiftId = await OpenHandler().Handle(
            new OpenShiftCommand(2000m), CancellationToken.None);

        var handler = new RecordDrawerMovementCommandHandler(_shifts, _uow, _user);
        var id = await handler.Handle(
            new RecordDrawerMovementCommand(-1000m, "Rema Drinks"), CancellationToken.None);

        var stored = await _ctx.CashDrawerMovements.AsNoTracking().SingleAsync(m => m.Id == id);
        Assert.Equal(shiftId, stored.ShiftId);
        Assert.Equal(-1000m, stored.Amount);
        Assert.Equal("Rema Drinks", stored.Note);
        Assert.False(stored.IsVoided);
    }

    [Fact]
    public async Task A_drawer_movement_needs_an_open_shift()
    {
        var handler = new RecordDrawerMovementCommandHandler(_shifts, _uow, _user);

        await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(
                new RecordDrawerMovementCommand(-500m, "Ice"), CancellationToken.None));
    }

    [Fact]
    public async Task A_voided_movement_stays_visible()
    {
        await OpenHandler().Handle(new OpenShiftCommand(2000m), CancellationToken.None);
        var record = new RecordDrawerMovementCommandHandler(_shifts, _uow, _user);
        var id = await record.Handle(
            new RecordDrawerMovementCommand(-1000m, "Rema Drinks"), CancellationToken.None);

        var voidHandler = new VoidDrawerMovementCommandHandler(_shifts, _uow, _user);
        await voidHandler.Handle(new VoidDrawerMovementCommand(id), CancellationToken.None);

        var stored = await _ctx.CashDrawerMovements.AsNoTracking().SingleAsync(m => m.Id == id);
        Assert.True(stored.IsVoided);
        Assert.NotNull(stored.VoidedAt);
        Assert.Equal(-1000m, stored.Amount);
    }

    [Fact]
    public async Task A_blank_note_is_rejected()
    {
        var validator = new RecordDrawerMovementCommandValidator();

        var result = await validator.ValidateAsync(
            new RecordDrawerMovementCommand(-1000m, "   "));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Closing_freezes_the_snapshot_and_computes_expected_cash()
    {
        var shiftId = await OpenHandler().Handle(
            new OpenShiftCommand(2000m), CancellationToken.None);
        var item = await SeedItemAsync("Kopiko Blanca", price: 10m);
        await SaleHandler().Handle(SaleOf(item, 5), CancellationToken.None);

        var record = new RecordDrawerMovementCommandHandler(_shifts, _uow, _user);
        await record.Handle(
            new RecordDrawerMovementCommand(-1000m, "Rema Drinks"), CancellationToken.None);
        await record.Handle(
            new RecordDrawerMovementCommand(2000m, "Change fund"), CancellationToken.None);

        await CloseHandler().Handle(
            new CloseShiftCommand(shiftId, 3050m), CancellationToken.None);

        var stored = await _ctx.Shifts.AsNoTracking().SingleAsync(s => s.Id == shiftId);
        Assert.Equal(ShiftStatus.Closed, stored.Status);
        Assert.NotNull(stored.Snapshot);
        Assert.Equal(50m, stored.Snapshot!.NetSales);
        Assert.Equal(1000m, stored.Snapshot.DrawerMovementsNet);
        Assert.Equal(3050m, stored.Snapshot.ExpectedCash);
        Assert.Equal(0m, stored.Snapshot.CashVariance);
    }

    [Fact]
    public async Task A_voided_drawer_movement_is_excluded_from_expected_cash()
    {
        var shiftId = await OpenHandler().Handle(
            new OpenShiftCommand(2000m), CancellationToken.None);

        var record = new RecordDrawerMovementCommandHandler(_shifts, _uow, _user);
        var id = await record.Handle(
            new RecordDrawerMovementCommand(-1000m, "Wrong amount"), CancellationToken.None);
        await new VoidDrawerMovementCommandHandler(_shifts, _uow, _user)
            .Handle(new VoidDrawerMovementCommand(id), CancellationToken.None);

        await CloseHandler().Handle(
            new CloseShiftCommand(shiftId, 2000m), CancellationToken.None);

        var stored = await _ctx.Shifts.AsNoTracking().SingleAsync(s => s.Id == shiftId);
        Assert.Equal(0m, stored.Snapshot!.DrawerMovementsNet);
        Assert.Equal(2000m, stored.Snapshot.ExpectedCash);
    }

    [Fact]
    public async Task A_short_drawer_records_a_negative_variance()
    {
        var shiftId = await OpenHandler().Handle(
            new OpenShiftCommand(2000m), CancellationToken.None);

        await CloseHandler().Handle(
            new CloseShiftCommand(shiftId, 1500m), CancellationToken.None);

        var stored = await _ctx.Shifts.AsNoTracking().SingleAsync(s => s.Id == shiftId);
        Assert.Equal(-500m, stored.Snapshot!.CashVariance);
    }

    [Fact]
    public async Task A_void_after_close_leaves_the_snapshot_untouched_and_hits_the_current_shift()
    {
        var mondayId = await OpenHandler().Handle(
            new OpenShiftCommand(2000m), CancellationToken.None);
        var item = await SeedItemAsync("Kopiko Blanca", price: 10m);
        var sale = await SaleHandler().Handle(SaleOf(item, 5), CancellationToken.None);

        await CloseHandler().Handle(
            new CloseShiftCommand(mondayId, 2050m), CancellationToken.None);

        var tuesdayId = await OpenHandler().Handle(
            new OpenShiftCommand(2000m), CancellationToken.None);

        _ctx.Sales.Add(new Sale
        {
            ReceiptNumber = "R-VOID-0001",
            Subtotal = -50m,
            Total = -50m,
            PaymentMethodId = PaymentMethodIds.Cash,
            RefundedFromId = sale.SaleId,
            ShiftId = tuesdayId,
            CreatedBy = _user.Id
        });
        await _ctx.SaveChangesAsync();

        var monday = await _ctx.Shifts.AsNoTracking().SingleAsync(s => s.Id == mondayId);
        Assert.Equal(50m, monday.Snapshot!.NetSales);
        Assert.Equal(2050m, monday.Snapshot.ExpectedCash);

        var tuesdaySales = await _sales.GetByShiftAsync(tuesdayId);
        Assert.Equal(-50m, PaidSales.Net(tuesdaySales));
    }

    [Fact]
    public async Task Shift_numbers_increment_across_shifts()
    {
        var firstId = await OpenHandler().Handle(
            new OpenShiftCommand(2000m), CancellationToken.None);
        await CloseHandler().Handle(
            new CloseShiftCommand(firstId, 2000m), CancellationToken.None);

        var secondId = await OpenHandler().Handle(
            new OpenShiftCommand(2000m), CancellationToken.None);

        var second = await _ctx.Shifts.AsNoTracking().SingleAsync(s => s.Id == secondId);
        Assert.Equal(2, second.Number);
    }

    [Fact]
    public async Task A_drawer_movement_cannot_be_recorded_against_a_closed_shift()
    {
        var shiftId = await OpenHandler().Handle(
            new OpenShiftCommand(2000m), CancellationToken.None);
        await CloseHandler().Handle(
            new CloseShiftCommand(shiftId, 2000m), CancellationToken.None);

        var handler = new RecordDrawerMovementCommandHandler(_shifts, _uow, _user);

        await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(
                new RecordDrawerMovementCommand(-500m, "too late"), CancellationToken.None));
    }

    [Fact]
    public async Task Starting_cash_is_correctable_while_the_shift_is_open()
    {
        var shiftId = await OpenHandler().Handle(
            new OpenShiftCommand(1500m), CancellationToken.None);

        var handler = new UpdateStartingCashCommandHandler(_shifts, _uow, _user);
        await handler.Handle(
            new UpdateStartingCashCommand(shiftId, 500m, "keyed the wrong float"),
            CancellationToken.None);

        var stored = await _ctx.Shifts.AsNoTracking().SingleAsync(s => s.Id == shiftId);
        Assert.Equal(500m, stored.StartingCash);
        Assert.Equal(1500m, stored.StartingCashOriginal);
        Assert.Equal("keyed the wrong float", stored.StartingCashCorrectionReason);
        Assert.NotNull(stored.StartingCashCorrectedAt);
    }

    [Fact]
    public async Task Starting_cash_is_locked_once_the_shift_closes()
    {
        var shiftId = await OpenHandler().Handle(
            new OpenShiftCommand(1500m), CancellationToken.None);
        await CloseHandler().Handle(
            new CloseShiftCommand(shiftId, 1500m), CancellationToken.None);

        var handler = new UpdateStartingCashCommandHandler(_shifts, _uow, _user);

        await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(
                new UpdateStartingCashCommand(shiftId, 500m, "too late"),
                CancellationToken.None));
    }

    [Fact]
    public async Task Correcting_the_count_preserves_the_original_and_recomputes_variance()
    {
        var shiftId = await OpenHandler().Handle(
            new OpenShiftCommand(2000m), CancellationToken.None);
        await CloseHandler().Handle(
            new CloseShiftCommand(shiftId, 9000m), CancellationToken.None);

        var handler = new CorrectShiftCountCommandHandler(_shifts, _uow, _user);
        await handler.Handle(
            new CorrectShiftCountCommand(shiftId, 2000m, "fat-fingered the count"),
            CancellationToken.None);

        var stored = await _ctx.Shifts.AsNoTracking().SingleAsync(s => s.Id == shiftId);
        Assert.Equal(2000m, stored.Snapshot!.CountedCash);
        Assert.Equal(9000m, stored.Snapshot.CountedCashOriginal);
        Assert.Equal(0m, stored.Snapshot.CashVariance);
        Assert.Equal("fat-fingered the count", stored.Snapshot.CorrectionReason);
    }

    [Fact]
    public async Task The_count_cannot_be_corrected_while_the_shift_is_open()
    {
        var shiftId = await OpenHandler().Handle(
            new OpenShiftCommand(2000m), CancellationToken.None);

        var handler = new CorrectShiftCountCommandHandler(_shifts, _uow, _user);

        await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(
                new CorrectShiftCountCommand(shiftId, 2000m, "not yet"),
                CancellationToken.None));
    }

    [Fact]
    public async Task An_open_shift_reads_live_and_a_closed_shift_reads_frozen()
    {
        var shiftId = await OpenHandler().Handle(
            new OpenShiftCommand(2000m), CancellationToken.None);
        var item = await SeedItemAsync("Kopiko Blanca", price: 10m);
        await SaleHandler().Handle(SaleOf(item, 5), CancellationToken.None);

        var query = new GetShiftReadQueryHandler(_shifts, _sales, _paymentMethods);

        var live = await query.Handle(new GetShiftReadQuery(shiftId), CancellationToken.None);
        Assert.False(live.IsClosed);
        Assert.Equal(50m, live.NetSales);
        Assert.Equal(2050m, live.ExpectedCash);
        Assert.Null(live.CountedCash);

        await CloseHandler().Handle(
            new CloseShiftCommand(shiftId, 2050m), CancellationToken.None);

        var frozen = await query.Handle(new GetShiftReadQuery(shiftId), CancellationToken.None);
        Assert.True(frozen.IsClosed);
        Assert.Equal(50m, frozen.NetSales);
        Assert.Equal(2050m, frozen.CountedCash);
        Assert.Equal(0m, frozen.CashVariance);
    }

    [Fact]
    public async Task Current_shift_returns_null_when_none_is_open()
    {
        var handler = new GetCurrentShiftQueryHandler(_shifts, _sales, _paymentMethods);

        var result = await handler.Handle(new GetCurrentShiftQuery(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task X_read_lists_active_sales_methods_with_zero_rows()
    {
        var shiftId = await OpenHandler().Handle(
            new OpenShiftCommand(2000m), CancellationToken.None);
        var item = await SeedItemAsync("Kopiko Blanca", price: 10m);
        await SaleHandler().Handle(SaleOf(item, 5), CancellationToken.None);

        await CloseHandler().Handle(
            new CloseShiftCommand(shiftId, 2050m), CancellationToken.None);

        var stored = await _ctx.Shifts
            .Include(s => s.MethodSales)
            .AsNoTracking().SingleAsync(s => s.Id == shiftId);

        Assert.Equal(3, stored.MethodSales.Count);
        Assert.Equal(
            50m, stored.MethodSales.Single(m => m.PaymentMethodId == PaymentMethodIds.Cash).Amount);
        Assert.Equal(
            0m, stored.MethodSales.Single(m => m.PaymentMethodId == PaymentMethodIds.GCash).Amount);
    }

    [Fact]
    public async Task Inactive_method_with_sales_still_gets_a_row()
    {
        var method = await new CreatePaymentMethodCommandHandler(_paymentMethods, _uow).Handle(
            new CreatePaymentMethodCommand("Bank transfer", false),
            CancellationToken.None);

        var shiftId = await OpenHandler().Handle(
            new OpenShiftCommand(2000m), CancellationToken.None);
        var item = await SeedItemAsync("Kopiko Blanca", price: 10m);
        await SaleHandler().Handle(
            new CreateSaleCommand(
                new List<CartItemInput> { new(item.Id, 2, 0m) },
                0m, method.Id, item.SellingPrice * 2),
            CancellationToken.None);

        await new UpdatePaymentMethodCommandHandler(_paymentMethods, _uow).Handle(
            new UpdatePaymentMethodCommand(method.Id, "Bank transfer", false, false),
            CancellationToken.None);

        await CloseHandler().Handle(
            new CloseShiftCommand(shiftId, 2000m), CancellationToken.None);

        var stored = await _ctx.Shifts
            .Include(s => s.MethodSales)
            .AsNoTracking().SingleAsync(s => s.Id == shiftId);

        var row = stored.MethodSales.Single(m => m.PaymentMethodId == method.Id);
        Assert.Equal(20m, row.Amount);
        Assert.Equal("Bank transfer", row.MethodName);
    }

    [Fact]
    public async Task Renaming_a_method_never_rewrites_a_frozen_x_read()
    {
        var firstId = await OpenHandler().Handle(
            new OpenShiftCommand(2000m), CancellationToken.None);
        var item = await SeedItemAsync("Kopiko Blanca", price: 10m);
        await SaleHandler().Handle(
            new CreateSaleCommand(
                new List<CartItemInput> { new(item.Id, 2, 0m) },
                0m, PaymentMethodIds.GCash, item.SellingPrice * 2, "REF-001"),
            CancellationToken.None);
        await CloseHandler().Handle(
            new CloseShiftCommand(firstId, 2000m), CancellationToken.None);

        await new UpdatePaymentMethodCommandHandler(_paymentMethods, _uow).Handle(
            new UpdatePaymentMethodCommand(PaymentMethodIds.GCash, "Wallet", true, true),
            CancellationToken.None);

        var secondId = await OpenHandler().Handle(
            new OpenShiftCommand(1000m), CancellationToken.None);

        var query = new GetShiftReadQueryHandler(_shifts, _sales, _paymentMethods);
        var frozen = await query.Handle(new GetShiftReadQuery(firstId), CancellationToken.None);
        var live = await query.Handle(new GetShiftReadQuery(secondId), CancellationToken.None);

        Assert.Equal(
            "GCash",
            frozen.MethodSales.Single(m => m.PaymentMethodId == PaymentMethodIds.GCash).Name);
        Assert.Equal(
            "Wallet",
            live.MethodSales.Single(m => m.PaymentMethodId == PaymentMethodIds.GCash).Name);
    }

    [Fact]
    public async Task Wallet_sales_feed_expected_cash()
    {
        var shiftId = await OpenHandler().Handle(
            new OpenShiftCommand(1500m), CancellationToken.None);
        var item = await SeedItemAsync("Kopiko Blanca", price: 5m);
        await SaleHandler().Handle(
            new CreateSaleCommand(
                new List<CartItemInput> { new(item.Id, 12, 0m) },
                0m, PaymentMethodIds.Cash, 60m),
            CancellationToken.None);
        await SaleHandler().Handle(
            new CreateSaleCommand(
                new List<CartItemInput> { new(item.Id, 8, 0m) },
                0m, PaymentMethodIds.GCash, 40m, "REF-100"),
            CancellationToken.None);

        var query = new GetShiftReadQueryHandler(_shifts, _sales, _paymentMethods);
        var live = await query.Handle(new GetShiftReadQuery(shiftId), CancellationToken.None);

        Assert.Equal(1600m, live.ExpectedCash);
    }

    [Fact]
    public async Task Custom_method_sales_feed_the_pool()
    {
        var method = await new CreatePaymentMethodCommandHandler(_paymentMethods, _uow).Handle(
            new CreatePaymentMethodCommand("Bank transfer", false),
            CancellationToken.None);

        var shiftId = await OpenHandler().Handle(
            new OpenShiftCommand(1500m), CancellationToken.None);
        var item = await SeedItemAsync("Kopiko Blanca", price: 5m);
        await SaleHandler().Handle(
            new CreateSaleCommand(
                new List<CartItemInput> { new(item.Id, 12, 0m) },
                0m, PaymentMethodIds.Cash, 60m),
            CancellationToken.None);
        await SaleHandler().Handle(
            new CreateSaleCommand(
                new List<CartItemInput> { new(item.Id, 8, 0m) },
                0m, PaymentMethodIds.GCash, 40m, "REF-101"),
            CancellationToken.None);
        await SaleHandler().Handle(
            new CreateSaleCommand(
                new List<CartItemInput> { new(item.Id, 5, 0m) },
                0m, method.Id, 25m),
            CancellationToken.None);

        var query = new GetShiftReadQueryHandler(_shifts, _sales, _paymentMethods);
        var live = await query.Handle(new GetShiftReadQuery(shiftId), CancellationToken.None);

        Assert.Equal(1625m, live.ExpectedCash);
        Assert.Equal(25m, live.MethodSales.Single(m => m.PaymentMethodId == method.Id).Amount);
    }

    [Fact]
    public async Task Close_takes_one_counted_figure_and_one_verdict()
    {
        var shiftId = await OpenHandler().Handle(
            new OpenShiftCommand(1500m), CancellationToken.None);
        var item = await SeedItemAsync("Kopiko Blanca", price: 5m);
        await SaleHandler().Handle(
            new CreateSaleCommand(
                new List<CartItemInput> { new(item.Id, 12, 0m) },
                0m, PaymentMethodIds.Cash, 60m),
            CancellationToken.None);
        await SaleHandler().Handle(
            new CreateSaleCommand(
                new List<CartItemInput> { new(item.Id, 8, 0m) },
                0m, PaymentMethodIds.GCash, 40m, "REF-102"),
            CancellationToken.None);

        await CloseHandler().Handle(
            new CloseShiftCommand(shiftId, 1595m), CancellationToken.None);

        var stored = await _ctx.Shifts.AsNoTracking().SingleAsync(s => s.Id == shiftId);
        Assert.Equal(1600m, stored.Snapshot!.ExpectedCash);
        Assert.Equal(1595m, stored.Snapshot.CountedCash);
        Assert.Equal(-5m, stored.Snapshot.CashVariance);
    }

    [Fact]
    public async Task Correct_count_recomputes_variance_and_says_x_read()
    {
        var shiftId = await OpenHandler().Handle(
            new OpenShiftCommand(2000m), CancellationToken.None);

        var handler = new CorrectShiftCountCommandHandler(_shifts, _uow, _user);
        var ex = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(
                new CorrectShiftCountCommand(shiftId, 2000m, "not yet"),
                CancellationToken.None));
        Assert.Contains("X read", ex.Message);

        await CloseHandler().Handle(
            new CloseShiftCommand(shiftId, 2500m), CancellationToken.None);
        await handler.Handle(
            new CorrectShiftCountCommand(shiftId, 1990m, "recount"),
            CancellationToken.None);

        var stored = await _ctx.Shifts.AsNoTracking().SingleAsync(s => s.Id == shiftId);
        Assert.Equal(1990m, stored.Snapshot!.CountedCash);
        Assert.Equal(-10m, stored.Snapshot.CashVariance);
    }

    [Fact]
    public async Task Expected_cash_sums_every_method_plus_movements()
    {
        await SeedOpenDayAsync();
        await OpenHandler().Handle(new OpenShiftCommand(1500m), CancellationToken.None);
        var shift = (await _shifts.GetOpenAsync())!;
        var item = await SeedItemAsync("Bread", price: 5m);

        await SaleHandler().Handle(new CreateSaleCommand(
            new List<CartItemInput> { new(item.Id, 12, 0m) }, 0m, PaymentMethodIds.Cash, 60m), default);
        await SaleHandler().Handle(new CreateSaleCommand(
            new List<CartItemInput> { new(item.Id, 8, 0m) }, 0m, PaymentMethodIds.GCash, 40m, "REF1"), default);
        await SaleHandler().Handle(new CreateSaleCommand(
            new List<CartItemInput> { new(item.Id, 5, 0m) }, 0m, PaymentMethodIds.Maya, 25m, "REF2"), default);
        await new RecordDrawerMovementCommandHandler(_shifts, _uow, _user).Handle(
            new RecordDrawerMovementCommand(10m, "Suki cash — Aling Rosa"), default);

        var read = await new GetShiftReadQueryHandler(_shifts, _sales, _paymentMethods)
            .Handle(new GetShiftReadQuery(shift.Id), default);
        Assert.Equal(1635m, read.ExpectedCash);
        Assert.Equal(3, read.MethodSales.Count);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }
}
