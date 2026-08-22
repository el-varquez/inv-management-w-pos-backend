using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Application.Sales.Commands.CreateTransaction;
using POS.Application.Sales.Commands.ProcessRefund;
using POS.Application.Sales.Queries.GetTransactionById;
using POS.Application.Shifts.Commands.CloseShift;
using POS.Application.Shifts.Queries.GetShiftRead;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Domain.Exceptions;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;
using POS.Infrastructure.Services;
using POS.Infrastructure.Tests.Fakes;
using Xunit;

namespace POS.Infrastructure.Tests;

public class RefundModuleTests : IDisposable
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
    private readonly FakeCurrentUser _user = new();
    private readonly Guid _categoryId = Guid.NewGuid();
    private readonly Shift _shift;

    public RefundModuleTests()
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
        _shift = new Shift
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
        _ctx.Shifts.Add(_shift);
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

    private CreateTransactionCommandHandler SaleHandler(
        POS.Application.Common.Interfaces.IReceiptNumberGenerator? generator = null) =>
        new(_items, _transactions, generator ?? new ReceiptNumberGenerator(_transactions), _uow,
            _user, _composites, _shifts, _settings, _utang);

    private ProcessRefundCommandHandler RefundHandler(
        POS.Application.Common.Interfaces.IReceiptNumberGenerator? generator = null) =>
        new(_transactions, generator ?? new ReceiptNumberGenerator(_transactions), _uow,
            _user, _shifts, _utang);

    private async Task<Guid> SellAsync(
        Item item, int qty = 1,
        POS.Application.Common.Interfaces.IReceiptNumberGenerator? generator = null)
    {
        var result = await SaleHandler(generator).Handle(
            new CreateTransactionCommand(
                [new CartItemInput(item.Id, qty, 0m)], 0m,
                PaymentType.Cash, item.SellingPrice * qty),
            CancellationToken.None);
        return result.TransactionId;
    }

    [Fact]
    public async Task A_refund_lands_in_the_open_shift()
    {
        var item = await SeedItemAsync();
        var saleId = await SellAsync(item);

        var result = await RefundHandler().Handle(
            new ProcessRefundCommand(saleId), CancellationToken.None);

        var mirror = await _ctx.Transactions.SingleAsync(t => t.Id == result.RefundTransactionId);
        Assert.Equal(_shift.Id, mirror.ShiftId);
        Assert.Equal(saleId, mirror.RefundedFromId);
    }

    [Fact]
    public async Task A_refund_with_no_open_shift_is_refused()
    {
        var item = await SeedItemAsync();
        var saleId = await SellAsync(item);
        _shift.Status = ShiftStatus.Closed;
        _ctx.SaveChanges();

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            RefundHandler().Handle(new ProcessRefundCommand(saleId), CancellationToken.None));

        Assert.Equal(
            "No open shift — voids land in the current shift. Declare starting cash first.",
            ex.Message);
        var original = await _ctx.Transactions.SingleAsync(t => t.Id == saleId);
        Assert.False(original.IsRefunded);
    }

    [Fact]
    public async Task A_sale_cannot_be_refunded_twice()
    {
        var item = await SeedItemAsync();
        var saleId = await SellAsync(item);
        await RefundHandler().Handle(new ProcessRefundCommand(saleId), CancellationToken.None);

        await Assert.ThrowsAsync<DomainException>(() =>
            RefundHandler().Handle(new ProcessRefundCommand(saleId), CancellationToken.None));
    }

    [Fact]
    public async Task A_receipt_collision_on_the_refund_is_retried()
    {
        var item = await SeedItemAsync();
        var saleId = await SellAsync(
            item, 1, new FakeCollidingReceiptNumberGenerator("R-20260820-0001"));

        var result = await RefundHandler(
                new FakeCollidingReceiptNumberGenerator("R-20260820-0001", "R-20260820-0002"))
            .Handle(new ProcessRefundCommand(saleId), CancellationToken.None);

        Assert.Equal("R-20260820-0002", result.ReceiptNumber);
        var mirror = await _ctx.Transactions.SingleAsync(t => t.Id == result.RefundTransactionId);
        Assert.Equal("R-20260820-0002", mirror.ReceiptNumber);
    }

    private CloseShiftCommandHandler CloseHandler()
        => new(_shifts, _transactions, _settings, _uow, _user, _utang);

    private GetShiftReadQueryHandler ReadHandler()
        => new(_shifts, _transactions, _utang);

    [Fact]
    public async Task The_live_x_read_reports_the_refund()
    {
        var item = await SeedItemAsync();
        var saleId = await SellAsync(item);
        await RefundHandler().Handle(new ProcessRefundCommand(saleId), CancellationToken.None);

        var read = await ReadHandler().Handle(
            new GetShiftReadQuery(_shift.Id), CancellationToken.None);

        Assert.Equal(25m, read.Refunds);
        Assert.Equal(1, read.RefundCount);
        Assert.Equal(0m, read.NetSales);
        Assert.Equal(0m, read.CashSales);
        Assert.Equal(1, read.TransactionCount);
    }

    [Fact]
    public async Task Closing_the_shift_freezes_the_refund_figures()
    {
        var item = await SeedItemAsync();
        var saleId = await SellAsync(item);
        await RefundHandler().Handle(new ProcessRefundCommand(saleId), CancellationToken.None);

        await CloseHandler().Handle(
            new CloseShiftCommand(_shift.Id, 1000m, null), CancellationToken.None);

        var read = await ReadHandler().Handle(
            new GetShiftReadQuery(_shift.Id), CancellationToken.None);
        Assert.True(read.IsClosed);
        Assert.Equal(25m, read.Refunds);
        Assert.Equal(1, read.RefundCount);
    }

    [Fact]
    public async Task A_refund_after_the_shift_closes_lands_in_the_next_shift()
    {
        var item = await SeedItemAsync();
        var saleId = await SellAsync(item);
        await CloseHandler().Handle(
            new CloseShiftCommand(_shift.Id, 1025m, null), CancellationToken.None);
        var second = new Shift
        {
            Number = 2,
            Status = ShiftStatus.Open,
            StartingCash = 500m,
            OpenedAt = DateTime.UtcNow,
            OpenedBy = _user.Id,
            BusinessDayId = _shift.BusinessDayId
        };
        _ctx.Shifts.Add(second);
        _ctx.SaveChanges();

        await RefundHandler().Handle(new ProcessRefundCommand(saleId), CancellationToken.None);

        var first = await ReadHandler().Handle(
            new GetShiftReadQuery(_shift.Id), CancellationToken.None);
        Assert.Equal(0m, first.Refunds);
        Assert.Equal(25m, first.NetSales);

        var current = await ReadHandler().Handle(
            new GetShiftReadQuery(second.Id), CancellationToken.None);
        Assert.Equal(25m, current.Refunds);
        Assert.Equal(1, current.RefundCount);
        Assert.Equal(-25m, current.NetSales);
        Assert.Equal(-25m, current.CashSales);
        Assert.Equal(0, current.TransactionCount);
    }

    [Fact]
    public async Task The_sale_detail_carries_the_payment_reference()
    {
        var item = await SeedItemAsync();
        var result = await SaleHandler().Handle(
            new CreateTransactionCommand(
                [new CartItemInput(item.Id, 1, 0m)], 0m,
                PaymentType.Gcash, 25m, "REF-777"),
            CancellationToken.None);

        var detail = await new GetTransactionByIdQueryHandler(_transactions).Handle(
            new GetTransactionByIdQuery(result.TransactionId), CancellationToken.None);

        Assert.Equal("REF-777", detail.ReferenceNumber);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }
}
