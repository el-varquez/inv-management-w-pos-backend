using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Application.Days.Commands.CloseDay;
using POS.Application.Days.Queries.GetCurrentDay;
using POS.Application.Sales.Commands.CreateTransaction;
using POS.Application.Shifts.Commands.CloseShift;
using POS.Application.Shifts.Commands.CorrectShiftCount;
using POS.Application.Shifts.Commands.OpenShift;
using POS.Application.Shifts.Commands.RecordDrawerMovement;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Domain.Exceptions;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;
using POS.Infrastructure.Tests.Fakes;
using Xunit;

namespace POS.Infrastructure.Tests;

public class DayModuleTests : IDisposable
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
    private readonly FakeReceiptNumberGenerator _receipts = new();
    private readonly Guid _categoryId = Guid.NewGuid();

    public DayModuleTests()
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

    private CloseShiftCommandHandler CloseHandler()
        => new(_shifts, _transactions, _settings, _uow, _user);

    private CloseDayCommandHandler CloseDayHandler()
        => new(_days, _shifts, _uow, _user);

    private CreateTransactionCommandHandler SaleHandler()
        => new(_items, _transactions, _receipts, _uow, _user,
            _composites, _shifts);

    private static CreateTransactionCommand SaleOf(Item item, int qty, PaymentType payment)
        => new(
            new List<CartItemInput> { new(item.Id, qty, 0m) },
            0m,
            payment,
            item.SellingPrice * qty);

    private async Task<Item> SeedItemAsync(string name, int stock = 100, decimal price = 10m)
    {
        var item = new Item
        {
            Name = name,
            ItemCode = $"D{Guid.NewGuid().ToString()[..4]}",
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
    public async Task Opening_the_first_shift_opens_a_day()
    {
        var shiftId = await OpenHandler().Handle(
            new OpenShiftCommand(2000m, null), CancellationToken.None);

        var day = await _ctx.BusinessDays.AsNoTracking().SingleAsync();
        Assert.Equal(1, day.Number);
        Assert.Equal(DayStatus.Open, day.Status);
        Assert.Equal(_user.Id, day.OpenedBy);

        var shift = await _ctx.Shifts.AsNoTracking().SingleAsync(s => s.Id == shiftId);
        Assert.Equal(day.Id, shift.BusinessDayId);
    }

    [Fact]
    public async Task A_second_shift_joins_the_open_day()
    {
        var firstId = await OpenHandler().Handle(
            new OpenShiftCommand(2000m, null), CancellationToken.None);
        await CloseHandler().Handle(
            new CloseShiftCommand(firstId, 2000m, null), CancellationToken.None);

        var secondId = await OpenHandler().Handle(
            new OpenShiftCommand(1000m, null), CancellationToken.None);

        var day = await _ctx.BusinessDays.AsNoTracking().SingleAsync();
        var second = await _ctx.Shifts.AsNoTracking().SingleAsync(s => s.Id == secondId);
        Assert.Equal(day.Id, second.BusinessDayId);
    }

    [Fact]
    public async Task Closing_a_shift_leaves_the_day_open()
    {
        var shiftId = await OpenHandler().Handle(
            new OpenShiftCommand(2000m, null), CancellationToken.None);
        await CloseHandler().Handle(
            new CloseShiftCommand(shiftId, 2000m, null), CancellationToken.None);

        var day = await _ctx.BusinessDays.AsNoTracking().SingleAsync();
        Assert.Equal(DayStatus.Open, day.Status);
        Assert.Null(day.Snapshot);
    }

    [Fact]
    public async Task Close_day_is_blocked_while_a_shift_is_open()
    {
        await OpenHandler().Handle(new OpenShiftCommand(2000m, null), CancellationToken.None);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => CloseDayHandler().Handle(new CloseDayCommand(), CancellationToken.None));

        Assert.Equal(
            "Shift #1 is still open — close it with an X read before closing the day.",
            ex.Message);
    }

    [Fact]
    public async Task Close_day_with_no_open_day_throws()
    {
        var ex = await Assert.ThrowsAsync<DomainException>(
            () => CloseDayHandler().Handle(new CloseDayCommand(), CancellationToken.None));

        Assert.Equal("No day is open — nothing to close.", ex.Message);
    }

    [Fact]
    public async Task Close_day_aggregates_its_shifts()
    {
        var item = await SeedItemAsync("Kopiko Blanca", price: 10m);
        var record = new RecordDrawerMovementCommandHandler(_shifts, _uow, _user);

        var firstId = await OpenHandler().Handle(
            new OpenShiftCommand(2000m, null), CancellationToken.None);
        await SaleHandler().Handle(SaleOf(item, 5, PaymentType.Cash), CancellationToken.None);
        await SaleHandler().Handle(SaleOf(item, 3, PaymentType.Gcash), CancellationToken.None);
        await record.Handle(
            new RecordDrawerMovementCommand(500m, "Change fund"), CancellationToken.None);
        await CloseHandler().Handle(
            new CloseShiftCommand(firstId, 2550m, null), CancellationToken.None);

        var secondId = await OpenHandler().Handle(
            new OpenShiftCommand(1000m, null), CancellationToken.None);
        await SaleHandler().Handle(SaleOf(item, 1, PaymentType.Cash), CancellationToken.None);
        await SaleHandler().Handle(SaleOf(item, 2, PaymentType.Maya), CancellationToken.None);
        await record.Handle(
            new RecordDrawerMovementCommand(-200m, "Rema Drinks"), CancellationToken.None);
        await CloseHandler().Handle(
            new CloseShiftCommand(secondId, 790m, null), CancellationToken.None);

        await CloseDayHandler().Handle(new CloseDayCommand(), CancellationToken.None);

        var day = await _ctx.BusinessDays.AsNoTracking().SingleAsync();
        Assert.Equal(DayStatus.Closed, day.Status);
        Assert.NotNull(day.Snapshot);
        Assert.Equal(110m, day.Snapshot!.NetSales);
        Assert.Equal(4, day.Snapshot.TransactionCount);
        Assert.Equal(60m, day.Snapshot.CashSales);
        Assert.Equal(30m, day.Snapshot.GcashSales);
        Assert.Equal(20m, day.Snapshot.MayaSales);
        Assert.Equal(300m, day.Snapshot.DrawerMovementsNet);
        Assert.Equal(790m, day.Snapshot.CountedCash);
        Assert.Equal(-20m, day.Snapshot.CashVariance);
        Assert.Equal(2, day.Snapshot.ShiftCount);
        Assert.Equal(_user.Id, day.ClosedBy);
        Assert.NotNull(day.ClosedAt);
    }

    [Fact]
    public async Task Day_closed_after_midnight_is_late()
    {
        var shiftId = await OpenHandler().Handle(
            new OpenShiftCommand(2000m, null), CancellationToken.None);
        await CloseHandler().Handle(
            new CloseShiftCommand(shiftId, 2000m, null), CancellationToken.None);

        var day = await _ctx.BusinessDays.SingleAsync();
        day.OpenedAt = DateTime.UtcNow.AddDays(-1);
        await _ctx.SaveChangesAsync();

        await CloseDayHandler().Handle(new CloseDayCommand(), CancellationToken.None);

        var stored = await _ctx.BusinessDays.AsNoTracking().SingleAsync();
        Assert.True(stored.ClosedLate);
    }

    [Fact]
    public async Task A_new_shift_after_day_close_opens_a_new_day()
    {
        var firstId = await OpenHandler().Handle(
            new OpenShiftCommand(2000m, null), CancellationToken.None);
        await CloseHandler().Handle(
            new CloseShiftCommand(firstId, 2000m, null), CancellationToken.None);
        await CloseDayHandler().Handle(new CloseDayCommand(), CancellationToken.None);

        var secondId = await OpenHandler().Handle(
            new OpenShiftCommand(2000m, null), CancellationToken.None);

        var second = await _ctx.Shifts.AsNoTracking().SingleAsync(s => s.Id == secondId);
        var newDay = await _ctx.BusinessDays.AsNoTracking()
            .SingleAsync(d => d.Id == second.BusinessDayId);
        Assert.Equal(2, newDay.Number);
        Assert.Equal(DayStatus.Open, newDay.Status);
    }

    [Fact]
    public async Task Current_day_read_includes_the_open_shifts_live_figures()
    {
        var firstId = await OpenHandler().Handle(
            new OpenShiftCommand(2000m, null), CancellationToken.None);
        var item = await SeedItemAsync("Kopiko Blanca", price: 10m);
        await SaleHandler().Handle(SaleOf(item, 5, PaymentType.Cash), CancellationToken.None);
        await CloseHandler().Handle(
            new CloseShiftCommand(firstId, 2050m, null), CancellationToken.None);

        await OpenHandler().Handle(new OpenShiftCommand(1000m, null), CancellationToken.None);
        await SaleHandler().Handle(SaleOf(item, 2, PaymentType.Cash), CancellationToken.None);

        var query = new GetCurrentDayQueryHandler(_days, _shifts, _transactions);
        var read = await query.Handle(new GetCurrentDayQuery(), CancellationToken.None);

        Assert.NotNull(read);
        Assert.False(read!.IsClosed);
        Assert.Equal(70m, read.NetSales);
        Assert.Equal(2, read.TransactionCount);
        Assert.Equal(2, read.ShiftCount);
        Assert.Equal(2050m, read.CountedCash);
        Assert.Equal(0m, read.CashVariance);
        Assert.Equal(2, read.Shifts.Count);
        Assert.False(read.Shifts[1].IsClosed);
        Assert.Equal(20m, read.Shifts[1].NetSales);
    }

    [Fact]
    public async Task Correct_count_after_day_close_leaves_the_frozen_day_untouched()
    {
        var firstId = await OpenHandler().Handle(
            new OpenShiftCommand(2000m, null), CancellationToken.None);
        await CloseHandler().Handle(
            new CloseShiftCommand(firstId, 2000m, null), CancellationToken.None);

        var secondId = await OpenHandler().Handle(
            new OpenShiftCommand(1000m, null), CancellationToken.None);
        await CloseHandler().Handle(
            new CloseShiftCommand(secondId, 9000m, null), CancellationToken.None);

        await CloseDayHandler().Handle(new CloseDayCommand(), CancellationToken.None);

        var correct = new CorrectShiftCountCommandHandler(_shifts, _uow, _user);
        await correct.Handle(
            new CorrectShiftCountCommand(secondId, 1000m, "fat-fingered the count"),
            CancellationToken.None);

        var shift = await _ctx.Shifts.AsNoTracking().SingleAsync(s => s.Id == secondId);
        Assert.Equal(1000m, shift.Snapshot!.CountedCash);
        Assert.Equal(0m, shift.Snapshot.CashVariance);

        var day = await _ctx.BusinessDays.AsNoTracking().SingleAsync();
        Assert.Equal(9000m, day.Snapshot!.CountedCash);
        Assert.Equal(8000m, day.Snapshot.CashVariance);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }
}
