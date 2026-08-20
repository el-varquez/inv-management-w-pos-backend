using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common;
using POS.Application.Sales.Commands.CreateTransaction;
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
    private readonly TransactionRepository _transactions;
    private readonly CompositeItemRepository _composites;
    private readonly StoreSettingsRepository _settings;
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

        _shifts = new ShiftRepository(_ctx);
        _days = new BusinessDayRepository(_ctx);
        _items = new ItemRepository(_ctx);
        _transactions = new TransactionRepository(_ctx);
        _composites = new CompositeItemRepository(_ctx);
        _settings = new StoreSettingsRepository(_ctx);
        _uow = new UnitOfWork(_ctx);

        _ctx.Categories.Add(new Category { Id = _categoryId, Name = "General" });
        _ctx.SaveChanges();
    }

    private OpenShiftCommandHandler OpenHandler()
        => new(_shifts, _days, _settings, _uow, _user);

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
        => new(_shifts, _transactions, _settings, _uow, _user);

    private CreateTransactionCommandHandler SaleHandler()
        => new(_items, _transactions, new FakeReceiptNumberGenerator(), _uow, _user,
            _composites, _shifts);

    private static CreateTransactionCommand SaleOf(Item item, int qty)
        => new(
            new List<CartItemInput> { new(item.Id, qty, 0m) },
            0m,
            PaymentType.Cash,
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
                CashSales = 3480m,
                GcashSales = 840m,
                MayaSales = 0m,
                DrawerMovementsNet = -1350m,
                ExpectedCash = 4130m,
                CountedCash = 4130m,
                CashVariance = 0m
            }
        };

        await _shifts.AddAsync(shift);
        await _uow.SaveChangesAsync();

        var stored = await _ctx.Shifts.AsNoTracking().SingleAsync();
        Assert.Equal(1, stored.Number);
        Assert.Equal(ShiftStatus.Closed, stored.Status);
        Assert.NotNull(stored.Snapshot);
        Assert.Equal(4320m, stored.Snapshot!.NetSales);
        Assert.Equal(-1350m, stored.Snapshot.DrawerMovementsNet);
    }

    [Fact]
    public async Task Opening_a_shift_numbers_it_from_one()
    {
        var id = await OpenHandler().Handle(
            new OpenShiftCommand(2000m, null), CancellationToken.None);

        var stored = await _ctx.Shifts.AsNoTracking().SingleAsync(s => s.Id == id);
        Assert.Equal(1, stored.Number);
        Assert.Equal(ShiftStatus.Open, stored.Status);
        Assert.Equal(2000m, stored.StartingCash);
        Assert.Equal(_user.Id, stored.OpenedBy);
    }

    [Fact]
    public async Task Opening_while_a_shift_is_open_is_rejected_by_number()
    {
        await OpenHandler().Handle(new OpenShiftCommand(2000m, null), CancellationToken.None);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => OpenHandler().Handle(new OpenShiftCommand(1500m, null), CancellationToken.None));

        Assert.Contains("#1", ex.Message);
    }

    [Fact]
    public async Task Opening_requires_a_starting_gcash_balance_when_wallet_tracking_is_on()
    {
        _ctx.StoreSettings.Add(new StoreSettings { TrackEWalletFloat = true });
        await _ctx.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => OpenHandler().Handle(new OpenShiftCommand(2000m, null), CancellationToken.None));

        Assert.Contains("e-wallet", ex.Message);
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
            new OpenShiftCommand(2000m, null), CancellationToken.None);
        var item = await SeedItemAsync("Kopiko Blanca");

        var result = await SaleHandler().Handle(SaleOf(item, 2), CancellationToken.None);

        var stored = await _ctx.Transactions.AsNoTracking()
            .SingleAsync(t => t.Id == result.TransactionId);
        Assert.Equal(shiftId, stored.ShiftId);
    }

    [Fact]
    public async Task A_payout_is_recorded_against_the_open_shift_with_its_note()
    {
        var shiftId = await OpenHandler().Handle(
            new OpenShiftCommand(2000m, null), CancellationToken.None);

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
        await OpenHandler().Handle(new OpenShiftCommand(2000m, null), CancellationToken.None);
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
            new OpenShiftCommand(2000m, null), CancellationToken.None);
        var item = await SeedItemAsync("Kopiko Blanca", price: 10m);
        await SaleHandler().Handle(SaleOf(item, 5), CancellationToken.None);

        var record = new RecordDrawerMovementCommandHandler(_shifts, _uow, _user);
        await record.Handle(
            new RecordDrawerMovementCommand(-1000m, "Rema Drinks"), CancellationToken.None);
        await record.Handle(
            new RecordDrawerMovementCommand(2000m, "Change fund"), CancellationToken.None);

        await CloseHandler().Handle(
            new CloseShiftCommand(shiftId, 3050m, null), CancellationToken.None);

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
            new OpenShiftCommand(2000m, null), CancellationToken.None);

        var record = new RecordDrawerMovementCommandHandler(_shifts, _uow, _user);
        var id = await record.Handle(
            new RecordDrawerMovementCommand(-1000m, "Wrong amount"), CancellationToken.None);
        await new VoidDrawerMovementCommandHandler(_shifts, _uow, _user)
            .Handle(new VoidDrawerMovementCommand(id), CancellationToken.None);

        await CloseHandler().Handle(
            new CloseShiftCommand(shiftId, 2000m, null), CancellationToken.None);

        var stored = await _ctx.Shifts.AsNoTracking().SingleAsync(s => s.Id == shiftId);
        Assert.Equal(0m, stored.Snapshot!.DrawerMovementsNet);
        Assert.Equal(2000m, stored.Snapshot.ExpectedCash);
    }

    [Fact]
    public async Task A_short_drawer_records_a_negative_variance()
    {
        var shiftId = await OpenHandler().Handle(
            new OpenShiftCommand(2000m, null), CancellationToken.None);

        await CloseHandler().Handle(
            new CloseShiftCommand(shiftId, 1500m, null), CancellationToken.None);

        var stored = await _ctx.Shifts.AsNoTracking().SingleAsync(s => s.Id == shiftId);
        Assert.Equal(-500m, stored.Snapshot!.CashVariance);
    }

    [Fact]
    public async Task A_void_after_close_leaves_the_snapshot_untouched_and_hits_the_current_shift()
    {
        var mondayId = await OpenHandler().Handle(
            new OpenShiftCommand(2000m, null), CancellationToken.None);
        var item = await SeedItemAsync("Kopiko Blanca", price: 10m);
        var sale = await SaleHandler().Handle(SaleOf(item, 5), CancellationToken.None);

        await CloseHandler().Handle(
            new CloseShiftCommand(mondayId, 2050m, null), CancellationToken.None);

        var tuesdayId = await OpenHandler().Handle(
            new OpenShiftCommand(2000m, null), CancellationToken.None);

        _ctx.Transactions.Add(new Transaction
        {
            ReceiptNumber = "R-VOID-0001",
            Subtotal = -50m,
            Total = -50m,
            PaymentType = PaymentType.Cash,
            RefundedFromId = sale.TransactionId,
            ShiftId = tuesdayId,
            CreatedBy = _user.Id
        });
        await _ctx.SaveChangesAsync();

        var monday = await _ctx.Shifts.AsNoTracking().SingleAsync(s => s.Id == mondayId);
        Assert.Equal(50m, monday.Snapshot!.NetSales);
        Assert.Equal(2050m, monday.Snapshot.ExpectedCash);

        var tuesdayTransactions = await _transactions.GetByShiftAsync(tuesdayId);
        Assert.Equal(-50m, PaidSales.Net(tuesdayTransactions));
    }

    [Fact]
    public async Task Shift_numbers_increment_across_shifts()
    {
        var firstId = await OpenHandler().Handle(
            new OpenShiftCommand(2000m, null), CancellationToken.None);
        await CloseHandler().Handle(
            new CloseShiftCommand(firstId, 2000m, null), CancellationToken.None);

        var secondId = await OpenHandler().Handle(
            new OpenShiftCommand(2000m, null), CancellationToken.None);

        var second = await _ctx.Shifts.AsNoTracking().SingleAsync(s => s.Id == secondId);
        Assert.Equal(2, second.Number);
    }

    [Fact]
    public async Task A_drawer_movement_cannot_be_recorded_against_a_closed_shift()
    {
        var shiftId = await OpenHandler().Handle(
            new OpenShiftCommand(2000m, null), CancellationToken.None);
        await CloseHandler().Handle(
            new CloseShiftCommand(shiftId, 2000m, null), CancellationToken.None);

        var handler = new RecordDrawerMovementCommandHandler(_shifts, _uow, _user);

        await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(
                new RecordDrawerMovementCommand(-500m, "too late"), CancellationToken.None));
    }

    [Fact]
    public async Task Starting_cash_is_correctable_while_the_shift_is_open()
    {
        var shiftId = await OpenHandler().Handle(
            new OpenShiftCommand(1500m, null), CancellationToken.None);

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
            new OpenShiftCommand(1500m, null), CancellationToken.None);
        await CloseHandler().Handle(
            new CloseShiftCommand(shiftId, 1500m, null), CancellationToken.None);

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
            new OpenShiftCommand(2000m, null), CancellationToken.None);
        await CloseHandler().Handle(
            new CloseShiftCommand(shiftId, 9000m, null), CancellationToken.None);

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
            new OpenShiftCommand(2000m, null), CancellationToken.None);

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
            new OpenShiftCommand(2000m, null), CancellationToken.None);
        var item = await SeedItemAsync("Kopiko Blanca", price: 10m);
        await SaleHandler().Handle(SaleOf(item, 5), CancellationToken.None);

        var query = new GetShiftReadQueryHandler(_shifts, _transactions);

        var live = await query.Handle(new GetShiftReadQuery(shiftId), CancellationToken.None);
        Assert.False(live.IsClosed);
        Assert.Equal(50m, live.NetSales);
        Assert.Equal(2050m, live.ExpectedCash);
        Assert.Null(live.CountedCash);

        await CloseHandler().Handle(
            new CloseShiftCommand(shiftId, 2050m, null), CancellationToken.None);

        var frozen = await query.Handle(new GetShiftReadQuery(shiftId), CancellationToken.None);
        Assert.True(frozen.IsClosed);
        Assert.Equal(50m, frozen.NetSales);
        Assert.Equal(2050m, frozen.CountedCash);
        Assert.Equal(0m, frozen.CashVariance);
    }

    [Fact]
    public async Task Current_shift_returns_null_when_none_is_open()
    {
        var handler = new GetCurrentShiftQueryHandler(_shifts, _transactions);

        var result = await handler.Handle(new GetCurrentShiftQuery(), CancellationToken.None);

        Assert.Null(result);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }
}
