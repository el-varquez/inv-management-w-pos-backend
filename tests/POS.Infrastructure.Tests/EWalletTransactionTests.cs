using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Application.Sales.Commands.ProcessRefund;
using POS.Application.Shifts.Commands.CloseShift;
using POS.Application.Shifts.Commands.RecordEWalletTransaction;
using POS.Application.Shifts.Commands.VoidEWalletTransaction;
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

public class EWalletTransactionTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _ctx;
    private readonly ItemRepository _items;
    private readonly TransactionRepository _transactions;
    private readonly ShiftRepository _shifts;
    private readonly StoreSettingsRepository _settings;
    private readonly UnitOfWork _uow;
    private readonly FakeCurrentUser _user = new();
    private readonly Guid _categoryId = Guid.NewGuid();
    private readonly Shift _shift;
    private Item _feeItem = null!;

    public EWalletTransactionTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _ctx = new AppDbContext(options);
        _ctx.Database.EnsureCreated();

        _items = new ItemRepository(_ctx);
        _transactions = new TransactionRepository(_ctx);
        _shifts = new ShiftRepository(_ctx);
        _settings = new StoreSettingsRepository(_ctx);
        _uow = new UnitOfWork(_ctx);

        _ctx.Categories.Add(new Category { Id = _categoryId, Name = "General" });
        _shift = new Shift
        {
            Number = 1,
            Status = ShiftStatus.Open,
            StartingCash = 1000m,
            StartingEWalletBalance = 8000m,
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

    private async Task SeedSettingsAsync(bool trackFloat = true, bool withFeeItem = true)
    {
        _feeItem = new Item
        {
            Name = "E-wallet fee",
            ItemCode = "E0001",
            CostPrice = 0m,
            SellingPrice = 1m,
            Stock = 0,
            TracksStock = false,
            CategoryId = _categoryId
        };
        await _items.AddAsync(_feeItem);
        await _uow.SaveChangesAsync();

        _ctx.StoreSettings.Add(new StoreSettings
        {
            StoreName = "Test Store",
            TrackEWalletFloat = trackFloat,
            EWalletFeeItemId = withFeeItem ? _feeItem.Id : null
        });
        await _ctx.SaveChangesAsync();
    }

    private RecordEWalletTransactionCommandHandler Handler()
        => new(_shifts, _settings, _items, _transactions,
            new ReceiptNumberGenerator(_transactions), _uow, _user);

    [Fact]
    public async Task A_cash_in_moves_the_wallet_down_and_the_drawer_up()
    {
        await SeedSettingsAsync();

        var id = await Handler().Handle(
            new RecordEWalletTransactionCommand(EWalletDirection.CashIn, 1000m, 10m),
            CancellationToken.None);

        var tx = await _ctx.EWalletTransactions.SingleAsync(t => t.Id == id);
        Assert.Equal(_shift.Id, tx.ShiftId);
        Assert.Equal(1000m, tx.Principal);
        Assert.Equal(-1000m, tx.WalletDelta);
        Assert.Equal(1000m, tx.DrawerDelta);

        var fee = await _ctx.Transactions
            .Include(t => t.Items)
            .SingleAsync(t => t.Id == tx.FeeTransactionId);
        Assert.Equal(PaymentType.Cash, fee.PaymentType);
        Assert.Equal(10m, fee.Total);
        Assert.Equal(_shift.Id, fee.ShiftId);
        var line = Assert.Single(fee.Items);
        Assert.Equal(_feeItem.Id, line.ItemId);
        Assert.Equal(10, line.Quantity);
        Assert.Equal(1m, line.UnitPrice);
    }

    [Fact]
    public async Task A_cash_out_posts_the_full_principal_never_the_net()
    {
        await SeedSettingsAsync();

        var id = await Handler().Handle(
            new RecordEWalletTransactionCommand(EWalletDirection.CashOut, 1000m, 20m),
            CancellationToken.None);

        var tx = await _ctx.EWalletTransactions.SingleAsync(t => t.Id == id);
        Assert.Equal(1000m, tx.Principal);
        Assert.Equal(1000m, tx.WalletDelta);
        Assert.Equal(-1000m, tx.DrawerDelta);
    }

    [Fact]
    public async Task A_waived_fee_writes_no_fee_sale()
    {
        await SeedSettingsAsync();

        var id = await Handler().Handle(
            new RecordEWalletTransactionCommand(EWalletDirection.CashIn, 500m, 0m),
            CancellationToken.None);

        var tx = await _ctx.EWalletTransactions.SingleAsync(t => t.Id == id);
        Assert.Null(tx.FeeTransactionId);
        Assert.Empty(_ctx.Transactions);
    }

    [Fact]
    public async Task Recording_without_an_open_shift_is_refused()
    {
        await SeedSettingsAsync();
        _shift.Status = ShiftStatus.Closed;
        _ctx.SaveChanges();

        var ex = await Assert.ThrowsAsync<DomainException>(() => Handler().Handle(
            new RecordEWalletTransactionCommand(EWalletDirection.CashIn, 500m, 5m),
            CancellationToken.None));

        Assert.Equal(
            "No open shift — e-wallet transactions belong to an open shift.", ex.Message);
    }

    [Fact]
    public async Task Recording_while_the_float_is_off_is_refused()
    {
        await SeedSettingsAsync(trackFloat: false);

        var ex = await Assert.ThrowsAsync<DomainException>(() => Handler().Handle(
            new RecordEWalletTransactionCommand(EWalletDirection.CashIn, 500m, 5m),
            CancellationToken.None));

        Assert.Equal(
            "E-wallet tracking is off — turn it on in web admin Settings.", ex.Message);
    }

    [Fact]
    public async Task A_fee_with_no_fee_item_configured_is_refused()
    {
        await SeedSettingsAsync(withFeeItem: false);

        var ex = await Assert.ThrowsAsync<DomainException>(() => Handler().Handle(
            new RecordEWalletTransactionCommand(EWalletDirection.CashIn, 500m, 5m),
            CancellationToken.None));

        Assert.Equal(
            "No e-wallet fee item is set — choose one in web admin Settings.", ex.Message);
    }

    [Fact]
    public async Task A_fee_item_priced_off_one_peso_is_refused()
    {
        await SeedSettingsAsync();
        _feeItem.SellingPrice = 2m;
        _ctx.SaveChanges();

        var ex = await Assert.ThrowsAsync<DomainException>(() => Handler().Handle(
            new RecordEWalletTransactionCommand(EWalletDirection.CashIn, 500m, 5m),
            CancellationToken.None));

        Assert.Equal(
            "The e-wallet fee item must be priced at ₱1.00 — fix it in web admin Items.",
            ex.Message);
    }

    [Fact]
    public async Task Adjustment_is_not_accepted_yet()
    {
        var validator = new RecordEWalletTransactionCommandValidator();

        var result = validator.Validate(
            new RecordEWalletTransactionCommand(EWalletDirection.Adjustment, 100m, 0m));

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-5, 10)]
    [InlineData(1000, -1)]
    [InlineData(1000, 1000)]
    [InlineData(1000, 1500)]
    [InlineData(1000, 10.5)]
    public void Invalid_amounts_fail_validation(decimal principal, decimal fee)
    {
        var validator = new RecordEWalletTransactionCommandValidator();

        var result = validator.Validate(
            new RecordEWalletTransactionCommand(EWalletDirection.CashIn, principal, fee));

        Assert.False(result.IsValid);
    }

    private GetShiftReadQueryHandler ReadHandler()
        => new(_shifts, _transactions);

    private CloseShiftCommandHandler CloseHandler()
        => new(_shifts, _transactions, _settings, _uow, _user);

    [Fact]
    public async Task A_cash_in_raises_expected_cash_and_lowers_expected_wallet()
    {
        await SeedSettingsAsync();
        await Handler().Handle(
            new RecordEWalletTransactionCommand(EWalletDirection.CashIn, 1000m, 10m),
            CancellationToken.None);

        var read = await ReadHandler().Handle(
            new GetShiftReadQuery(_shift.Id), CancellationToken.None);

        Assert.Equal(1, read.EWalletCashInCount);
        Assert.Equal(1000m, read.EWalletCashIn);
        Assert.Equal(0, read.EWalletCashOutCount);
        Assert.Equal(2010m, read.ExpectedCash);
        Assert.Equal(7000m, read.ExpectedEWalletBalance);
        Assert.Equal(10m, read.CashSales);
        Assert.Equal(10m, read.NetSales);
    }

    [Fact]
    public async Task A_cash_out_lowers_expected_cash_and_raises_expected_wallet()
    {
        await SeedSettingsAsync();
        await Handler().Handle(
            new RecordEWalletTransactionCommand(EWalletDirection.CashOut, 1000m, 20m),
            CancellationToken.None);

        var read = await ReadHandler().Handle(
            new GetShiftReadQuery(_shift.Id), CancellationToken.None);

        Assert.Equal(1, read.EWalletCashOutCount);
        Assert.Equal(1000m, read.EWalletCashOut);
        Assert.Equal(20m, read.CashSales);
        Assert.Equal(20m, read.ExpectedCash);
        Assert.Equal(9000m, read.ExpectedEWalletBalance);
    }

    [Fact]
    public async Task Closing_the_shift_freezes_the_e_wallet_figures()
    {
        await SeedSettingsAsync();
        await Handler().Handle(
            new RecordEWalletTransactionCommand(EWalletDirection.CashIn, 1000m, 10m),
            CancellationToken.None);

        await CloseHandler().Handle(
            new CloseShiftCommand(_shift.Id, 2010m, 7000m), CancellationToken.None);

        var read = await ReadHandler().Handle(
            new GetShiftReadQuery(_shift.Id), CancellationToken.None);
        Assert.True(read.IsClosed);
        Assert.Equal(1, read.EWalletCashInCount);
        Assert.Equal(1000m, read.EWalletCashIn);
        Assert.Equal(2010m, read.ExpectedCash);
        Assert.Equal(7000m, read.ExpectedEWalletBalance);
        Assert.Equal(0m, read.CashVariance);
        Assert.Equal(0m, read.EWalletVariance);
    }

    private VoidEWalletTransactionCommandHandler VoidHandler()
        => new(_shifts, _transactions, _uow, _user);

    private ProcessRefundCommandHandler RefundHandler()
        => new(_transactions, new ReceiptNumberGenerator(_transactions), _uow, _user, _shifts);

    [Fact]
    public async Task Voiding_the_e_wallet_record_refunds_the_fee_sale()
    {
        await SeedSettingsAsync();
        var id = await Handler().Handle(
            new RecordEWalletTransactionCommand(EWalletDirection.CashIn, 1000m, 10m),
            CancellationToken.None);
        var feeId = (await _ctx.EWalletTransactions.SingleAsync(t => t.Id == id))
            .FeeTransactionId!.Value;

        await VoidHandler().Handle(
            new VoidEWalletTransactionCommand(id), CancellationToken.None);

        var tx = await _ctx.EWalletTransactions.SingleAsync(t => t.Id == id);
        Assert.True(tx.IsVoided);
        Assert.NotNull(tx.VoidedAt);
        var fee = await _ctx.Transactions.SingleAsync(t => t.Id == feeId);
        Assert.True(fee.IsRefunded);
    }

    [Fact]
    public async Task Refunding_the_fee_sale_voids_the_e_wallet_record()
    {
        await SeedSettingsAsync();
        var id = await Handler().Handle(
            new RecordEWalletTransactionCommand(EWalletDirection.CashOut, 1000m, 20m),
            CancellationToken.None);
        var feeId = (await _ctx.EWalletTransactions.SingleAsync(t => t.Id == id))
            .FeeTransactionId!.Value;

        await RefundHandler().Handle(new ProcessRefundCommand(feeId), CancellationToken.None);

        var tx = await _ctx.EWalletTransactions.SingleAsync(t => t.Id == id);
        Assert.True(tx.IsVoided);
    }

    [Fact]
    public async Task A_voided_record_drops_out_of_both_reconciliations()
    {
        await SeedSettingsAsync();
        var id = await Handler().Handle(
            new RecordEWalletTransactionCommand(EWalletDirection.CashIn, 1000m, 10m),
            CancellationToken.None);

        await VoidHandler().Handle(
            new VoidEWalletTransactionCommand(id), CancellationToken.None);

        var read = await ReadHandler().Handle(
            new GetShiftReadQuery(_shift.Id), CancellationToken.None);
        Assert.Equal(0, read.EWalletCashInCount);
        Assert.Equal(0m, read.EWalletCashIn);
        Assert.Equal(1000m, read.ExpectedCash);
        Assert.Equal(8000m, read.ExpectedEWalletBalance);
        Assert.Equal(0m, read.NetSales);
    }

    [Fact]
    public async Task Voiding_twice_is_refused()
    {
        await SeedSettingsAsync();
        var id = await Handler().Handle(
            new RecordEWalletTransactionCommand(EWalletDirection.CashIn, 500m, 5m),
            CancellationToken.None);
        await VoidHandler().Handle(
            new VoidEWalletTransactionCommand(id), CancellationToken.None);

        await Assert.ThrowsAsync<DomainException>(() => VoidHandler().Handle(
            new VoidEWalletTransactionCommand(id), CancellationToken.None));
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }
}
