using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common;
using POS.Application.Sales.Commands.CreateTransaction;
using POS.Application.Dashboard.Queries.GetDashboardSummary;
using POS.Application.Reports.Queries.GetBestSellers;
using POS.Application.Sales.Commands.ProcessRefund;
using POS.Application.Sales.Queries.GetSalesSummary;
using POS.Application.Shifts.Commands.CloseShift;
using POS.Application.Shifts.Queries.GetShiftRead;
using POS.Application.Utang.Commands.CollectUtangPayment;
using POS.Application.Utang.Commands.EditUtangPayment;
using POS.Application.Utang.Commands.VoidUtangPayment;
using POS.Application.Utang.Commands.CreateSuki;
using POS.Application.Utang.Queries.GetSukiLedger;
using POS.Application.Utang.Queries.GetSukis;
using POS.Application.Utang.Queries.GetUtangSummary;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Domain.Exceptions;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;
using POS.Infrastructure.Services;
using POS.Infrastructure.Tests.Fakes;
using Xunit;

namespace POS.Infrastructure.Tests;

public class UtangModuleTests : IDisposable
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

    public UtangModuleTests()
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

    private async Task SeedSettingsAsync(
        bool acceptUtang = true, decimal defaultMarkup = 1m)
    {
        _ctx.StoreSettings.Add(new StoreSettings
        {
            StoreName = "Test Store",
            AcceptUtang = acceptUtang,
            DefaultUtangMarkup = defaultMarkup
        });
        await _ctx.SaveChangesAsync();
    }

    private async Task<Suki> SeedSukiAsync(string name = "Aling Rosa")
    {
        var suki = new Suki { Name = name, CreatedBy = _user.Id };
        await _utang.AddSukiAsync(suki);
        await _uow.SaveChangesAsync();
        return suki;
    }

    private async Task<Item> SeedItemAsync(
        string name = "Coke Mismo 300ml", decimal price = 4m, decimal? markup = null)
    {
        var item = new Item
        {
            Name = name,
            ItemCode = $"X{_ctx.Items.Count() + 1:D4}",
            CostPrice = 2m,
            SellingPrice = price,
            UtangMarkup = markup,
            Stock = 50,
            CategoryId = _categoryId
        };
        await _items.AddAsync(item);
        await _uow.SaveChangesAsync();
        return item;
    }

    private async Task<Transaction> SeedSaleShellAsync()
    {
        var transaction = new Transaction
        {
            ReceiptNumber = $"R-TEST-{Guid.NewGuid():N}",
            PaymentType = PaymentType.Utang,
            CreatedBy = _user.Id,
            ShiftId = _shift.Id
        };
        _ctx.Transactions.Add(transaction);
        await _ctx.SaveChangesAsync();
        return transaction;
    }

    private async Task<UtangCharge> SeedChargeAsync(
        Guid sukiId, decimal amount, bool voided = false,
        DateTime? createdAt = null, decimal markup = 0m)
    {
        var charge = new UtangCharge
        {
            SukiId = sukiId,
            Amount = amount,
            Markup = markup,
            TransactionId = (await SeedSaleShellAsync()).Id,
            ShiftId = _shift.Id,
            IsVoided = voided,
            CreatedBy = _user.Id
        };
        if (createdAt is not null) charge.CreatedAt = createdAt.Value;
        await _utang.AddChargeAsync(charge);
        await _uow.SaveChangesAsync();
        return charge;
    }

    private async Task<UtangPayment> SeedPaymentAsync(
        Guid sukiId, decimal amount, bool voided = false,
        DateTime? createdAt = null)
    {
        var payment = new UtangPayment
        {
            SukiId = sukiId,
            Amount = amount,
            ShiftId = _shift.Id,
            IsVoided = voided,
            CreatedBy = _user.Id
        };
        if (createdAt is not null) payment.CreatedAt = createdAt.Value;
        await _utang.AddPaymentAsync(payment);
        await _uow.SaveChangesAsync();
        return payment;
    }

    [Fact]
    public async Task Creating_a_suki_trims_and_persists()
    {
        var handler = new CreateSukiCommandHandler(_utang, _uow, _user);

        var dto = await handler.Handle(
            new CreateSukiCommand("  Mang Tonyo  ", "  0917 555 2101  "),
            CancellationToken.None);

        Assert.Equal("Mang Tonyo", dto.Name);
        Assert.Equal("0917 555 2101", dto.Phone);
        Assert.Equal(0m, dto.Balance);
        Assert.Single(_ctx.Sukis);
    }

    [Fact]
    public async Task The_suki_list_computes_balances_and_filters_by_term()
    {
        var rosa = await SeedSukiAsync("Aling Rosa");
        var tonyo = await SeedSukiAsync("Mang Tonyo");
        await SeedChargeAsync(rosa.Id, 200m);
        await SeedPaymentAsync(rosa.Id, 50m);
        await SeedChargeAsync(tonyo.Id, 80m);

        var all = await new GetSukisQueryHandler(_utang).Handle(
            new GetSukisQuery(null, 1, 20), CancellationToken.None);
        Assert.Equal(2, all.TotalCount);
        Assert.Equal(150m, all.Items.Single(s => s.Name == "Aling Rosa").Balance);
        Assert.Equal(80m, all.Items.Single(s => s.Name == "Mang Tonyo").Balance);

        var filtered = await new GetSukisQueryHandler(_utang).Handle(
            new GetSukisQuery("tonyo", 1, 20), CancellationToken.None);
        Assert.Single(filtered.Items);
    }

    [Fact]
    public async Task The_balance_excludes_voided_entries()
    {
        var suki = await SeedSukiAsync();
        await SeedChargeAsync(suki.Id, 200m);
        await SeedChargeAsync(suki.Id, 100m, voided: true);
        await SeedPaymentAsync(suki.Id, 30m);
        await SeedPaymentAsync(suki.Id, 10m, voided: true);

        Assert.Equal(170m, await _utang.GetBalanceAsync(suki.Id));
    }

    [Fact]
    public async Task The_ledger_lists_entries_chronologically_with_markup_earned()
    {
        var suki = await SeedSukiAsync();
        await SeedChargeAsync(suki.Id, 200m, markup: 12m);
        await SeedPaymentAsync(suki.Id, 50m);

        var ledger = await new GetSukiLedgerQueryHandler(_utang).Handle(
            new GetSukiLedgerQuery(suki.Id), CancellationToken.None);

        Assert.Equal(150m, ledger.Balance);
        Assert.Equal(12m, ledger.MarkupEarned);
        Assert.Equal(2, ledger.Entries.Count);
        Assert.Equal("Charge", ledger.Entries[0].Type);
        Assert.Equal("Payment", ledger.Entries[1].Type);
        Assert.Null(ledger.Entries[0].Note);
        Assert.Equal("Payment received", ledger.Entries[1].Note);
    }

    private CreateTransactionCommandHandler SaleHandler() =>
        new(_items, _transactions, new ReceiptNumberGenerator(_transactions), _uow,
            _user, _composites, _shifts, _settings, _utang);

    private async Task<CreateTransactionResult> UtangSaleAsync(
        Suki suki, Item item, int qty = 1, decimal down = 0m)
        => await SaleHandler().Handle(
            new CreateTransactionCommand(
                [new CartItemInput(item.Id, qty, 0m)], 0m,
                PaymentType.Utang, 0m, null, suki.Id, down),
            CancellationToken.None);

    [Fact]
    public async Task An_utang_sale_reprices_lines_with_the_default_markup()
    {
        await SeedSettingsAsync(defaultMarkup: 1m);
        var suki = await SeedSukiAsync();
        var item = await SeedItemAsync(price: 4m);

        var result = await UtangSaleAsync(suki, item, qty: 3);

        Assert.Equal(15m, result.Total);
        var saved = await _ctx.Transactions
            .Include(t => t.Items)
            .SingleAsync(t => t.Id == result.TransactionId);
        Assert.Equal(5m, saved.Items.Single().UnitPrice);
        Assert.Equal(suki.Id, saved.SukiId);
        Assert.Equal(0m, saved.AmountTendered);
    }

    [Fact]
    public async Task Item_markup_overrides_and_zero_opts_out()
    {
        await SeedSettingsAsync(defaultMarkup: 1m);
        var suki = await SeedSukiAsync();
        var overridden = await SeedItemAsync("Rice 1 kg", price: 52m, markup: 3m);
        var optedOut = await SeedItemAsync("Eggs per pc", price: 8.5m, markup: 0m);

        var result = await SaleHandler().Handle(
            new CreateTransactionCommand(
                [new CartItemInput(overridden.Id, 1, 0m), new CartItemInput(optedOut.Id, 2, 0m)],
                0m, PaymentType.Utang, 0m, null, suki.Id),
            CancellationToken.None);

        Assert.Equal(72m, result.Total);
        var charge = await _ctx.UtangCharges.SingleAsync();
        Assert.Equal(3m, charge.Markup);
    }

    [Fact]
    public async Task The_charge_is_the_full_total_and_the_down_payment_is_a_linked_payment()
    {
        await SeedSettingsAsync();
        var suki = await SeedSukiAsync();
        var item = await SeedItemAsync(price: 40m);

        var result = await UtangSaleAsync(suki, item, qty: 5, down: 50m);

        var charge = Assert.Single(
            await _utang.GetChargesByTransactionAsync(result.TransactionId));
        var down = Assert.Single(
            await _utang.GetPaymentsByTransactionAsync(result.TransactionId));
        Assert.Equal(205m, charge.Amount);
        Assert.Equal(50m, down.Amount);
        Assert.Equal(_shift.Id, charge.ShiftId);
        Assert.Equal(155m, await _utang.GetBalanceAsync(suki.Id));

        var ledger = await new GetSukiLedgerQueryHandler(_utang).Handle(
            new GetSukiLedgerQuery(suki.Id), CancellationToken.None);
        Assert.Equal(
            "Down payment",
            ledger.Entries.Single(e => e.Type == "Payment").Note);
    }

    [Fact]
    public async Task Utang_is_refused_when_the_setting_is_off()
    {
        await SeedSettingsAsync(acceptUtang: false);
        var suki = await SeedSukiAsync();
        var item = await SeedItemAsync();

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => UtangSaleAsync(suki, item));
        Assert.Equal("Utang is off — turn it on in web admin Settings.", ex.Message);
    }

    [Fact]
    public async Task A_down_payment_covering_the_total_is_refused()
    {
        await SeedSettingsAsync();
        var suki = await SeedSukiAsync();
        var item = await SeedItemAsync(price: 4m);

        await Assert.ThrowsAsync<DomainException>(
            () => UtangSaleAsync(suki, item, qty: 1, down: 5m));
    }

    [Fact]
    public async Task Paid_sales_exclude_utang_and_its_mirrors()
    {
        var cash = new Transaction { Total = 100m, PaymentType = PaymentType.Cash };
        var utang = new Transaction { Total = 205m, PaymentType = PaymentType.Utang };
        var utangMirror = new Transaction
        {
            Total = -205m,
            PaymentType = PaymentType.Utang,
            RefundedFromId = utang.Id
        };
        var list = new List<Transaction> { cash, utang, utangMirror };

        Assert.Equal(100m, PaidSales.Net(list));
        Assert.Equal(1, PaidSales.Count(list));
        Assert.Equal(0m, PaidSales.Refunds(list));
        Assert.Equal(0, PaidSales.RefundCount(list));
    }

    private CollectUtangPaymentCommandHandler CollectHandler()
        => new(_utang, _shifts, _uow, _user);

    private ProcessRefundCommandHandler RefundHandler()
        => new(_transactions, new ReceiptNumberGenerator(_transactions), _uow,
            _user, _shifts, _utang);

    [Fact]
    public async Task Collecting_writes_a_payment_and_lowers_the_balance()
    {
        await SeedSettingsAsync();
        var suki = await SeedSukiAsync();
        await SeedChargeAsync(suki.Id, 200m);

        await CollectHandler().Handle(
            new CollectUtangPaymentCommand(suki.Id, 120m), CancellationToken.None);

        Assert.Equal(80m, await _utang.GetBalanceAsync(suki.Id));
        var payment = await _ctx.UtangPayments.SingleAsync();
        Assert.Equal(_shift.Id, payment.ShiftId);
        Assert.Null(payment.TransactionId);

        var ledger = await new GetSukiLedgerQueryHandler(_utang).Handle(
            new GetSukiLedgerQuery(suki.Id), CancellationToken.None);
        Assert.Equal(
            "Payment received",
            ledger.Entries.Single(e => e.Type == "Payment").Note);
    }

    [Fact]
    public async Task Collecting_more_than_the_balance_is_refused()
    {
        await SeedSettingsAsync();
        var suki = await SeedSukiAsync();
        await SeedChargeAsync(suki.Id, 100m);

        await Assert.ThrowsAsync<DomainException>(() => CollectHandler().Handle(
            new CollectUtangPaymentCommand(suki.Id, 150m), CancellationToken.None));
    }

    [Fact]
    public async Task Collecting_without_an_open_shift_is_refused()
    {
        await SeedSettingsAsync();
        var suki = await SeedSukiAsync();
        await SeedChargeAsync(suki.Id, 100m);
        _shift.Status = ShiftStatus.Closed;
        _ctx.SaveChanges();

        await Assert.ThrowsAsync<DomainException>(() => CollectHandler().Handle(
            new CollectUtangPaymentCommand(suki.Id, 50m), CancellationToken.None));
    }

    [Fact]
    public async Task Refunding_an_utang_sale_strikes_the_charge_and_its_down_payment()
    {
        await SeedSettingsAsync();
        var suki = await SeedSukiAsync();
        var item = await SeedItemAsync(price: 40m);
        var sale = await UtangSaleAsync(suki, item, qty: 5, down: 50m);

        await RefundHandler().Handle(
            new ProcessRefundCommand(sale.TransactionId), CancellationToken.None);

        var charges = await _utang.GetChargesByTransactionAsync(sale.TransactionId);
        var payments = await _utang.GetPaymentsByTransactionAsync(sale.TransactionId);
        Assert.All(charges, c => Assert.True(c.IsVoided));
        Assert.All(payments, p => Assert.True(p.IsVoided));
        Assert.Equal(0m, await _utang.GetBalanceAsync(suki.Id));
        Assert.Empty(_ctx.CashDrawerMovements);
    }

    [Fact]
    public async Task A_cross_shift_void_returns_the_down_payment_through_the_drawer()
    {
        await SeedSettingsAsync();
        var suki = await SeedSukiAsync();
        var item = await SeedItemAsync(price: 40m);
        var sale = await UtangSaleAsync(suki, item, qty: 5, down: 50m);
        _shift.Status = ShiftStatus.Closed;
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

        await RefundHandler().Handle(
            new ProcessRefundCommand(sale.TransactionId), CancellationToken.None);

        var movement = await _ctx.CashDrawerMovements.SingleAsync();
        Assert.Equal(second.Id, movement.ShiftId);
        Assert.Equal(-50m, movement.Amount);
        Assert.StartsWith("Utang void — down payment returned · ", movement.Note);
    }

    [Fact]
    public async Task A_charge_void_after_collections_may_leave_a_credit_balance()
    {
        await SeedSettingsAsync();
        var suki = await SeedSukiAsync();
        var item = await SeedItemAsync(price: 40m);
        var sale = await UtangSaleAsync(suki, item, qty: 5);
        await CollectHandler().Handle(
            new CollectUtangPaymentCommand(suki.Id, 100m), CancellationToken.None);

        await RefundHandler().Handle(
            new ProcessRefundCommand(sale.TransactionId), CancellationToken.None);

        Assert.Equal(-100m, await _utang.GetBalanceAsync(suki.Id));
    }

    [Fact]
    public async Task Payment_corrections_keep_the_first_original_and_charge_ids_are_unknown()
    {
        await SeedSettingsAsync();
        var suki = await SeedSukiAsync();
        var charge = await SeedChargeAsync(suki.Id, 200m);
        var payment = await SeedPaymentAsync(suki.Id, 100m);
        var edit = new EditUtangPaymentCommandHandler(_utang, _uow, _user);

        await edit.Handle(
            new EditUtangPaymentCommand(payment.Id, 80m), CancellationToken.None);
        await edit.Handle(
            new EditUtangPaymentCommand(payment.Id, 60m), CancellationToken.None);

        Assert.Equal(60m, payment.Amount);
        Assert.Equal(100m, payment.EditedFrom);
        await Assert.ThrowsAsync<NotFoundException>(() => edit.Handle(
            new EditUtangPaymentCommand(charge.Id, 50m), CancellationToken.None));

        var voidHandler = new VoidUtangPaymentCommandHandler(_utang, _uow, _user);
        await Assert.ThrowsAsync<NotFoundException>(() => voidHandler.Handle(
            new VoidUtangPaymentCommand(charge.Id), CancellationToken.None));
        await voidHandler.Handle(
            new VoidUtangPaymentCommand(payment.Id), CancellationToken.None);
        Assert.Equal(200m, await _utang.GetBalanceAsync(suki.Id));
    }

    private GetShiftReadQueryHandler ReadHandler()
        => new(_shifts, _transactions, _utang);

    private CloseShiftCommandHandler CloseHandler()
        => new(_shifts, _transactions, _settings, _uow, _user, _utang);

    [Fact]
    public async Task The_live_x_read_reports_utang_and_collections_feed_the_drawer()
    {
        await SeedSettingsAsync();
        var suki = await SeedSukiAsync();
        var item = await SeedItemAsync(price: 40m);
        await UtangSaleAsync(suki, item, qty: 5, down: 50m);
        await CollectHandler().Handle(
            new CollectUtangPaymentCommand(suki.Id, 30m), CancellationToken.None);

        var read = await ReadHandler().Handle(
            new GetShiftReadQuery(_shift.Id), CancellationToken.None);

        Assert.Equal(1, read.UtangChargedCount);
        Assert.Equal(205m, read.UtangCharged);
        Assert.Equal(5m, read.UtangMarkup);
        Assert.Equal(80m, read.UtangCollections);
        Assert.Equal(0m, read.NetSales);
        Assert.Equal(0, read.TransactionCount);
        Assert.Equal(1080m, read.ExpectedCash);
    }

    [Fact]
    public async Task Closing_the_shift_freezes_the_utang_figures()
    {
        await SeedSettingsAsync();
        var suki = await SeedSukiAsync();
        var item = await SeedItemAsync(price: 40m);
        await UtangSaleAsync(suki, item, qty: 5, down: 50m);

        await CloseHandler().Handle(
            new CloseShiftCommand(_shift.Id, 1050m, null), CancellationToken.None);

        var read = await ReadHandler().Handle(
            new GetShiftReadQuery(_shift.Id), CancellationToken.None);
        Assert.True(read.IsClosed);
        Assert.Equal(205m, read.UtangCharged);
        Assert.Equal(5m, read.UtangMarkup);
        Assert.Equal(50m, read.UtangCollections);
        Assert.Equal(1050m, read.ExpectedCash);
        Assert.Equal(0m, read.CashVariance);
    }

    [Fact]
    public async Task The_sales_summary_excludes_utang()
    {
        await SeedSettingsAsync();
        var suki = await SeedSukiAsync();
        var item = await SeedItemAsync(price: 40m);
        await UtangSaleAsync(suki, item, qty: 5);
        await SaleHandler().Handle(
            new CreateTransactionCommand(
                [new CartItemInput(item.Id, 1, 0m)], 0m, PaymentType.Cash, 40m),
            CancellationToken.None);

        var summary = await new GetSalesSummaryQueryHandler(_transactions).Handle(
            new GetSalesSummaryQuery(null, null), CancellationToken.None);

        Assert.Equal(40m, summary.NetSales);
        Assert.Equal(1, summary.TransactionCount);
    }

    [Fact]
    public async Task Best_sellers_count_utang_quantity_but_not_its_money()
    {
        await SeedSettingsAsync();
        var suki = await SeedSukiAsync();
        var item = await SeedItemAsync(price: 40m);
        await UtangSaleAsync(suki, item, qty: 5);
        await SaleHandler().Handle(
            new CreateTransactionCommand(
                [new CartItemInput(item.Id, 2, 0m)], 0m, PaymentType.Cash, 80m),
            CancellationToken.None);

        var best = await new GetBestSellersQueryHandler(_transactions).Handle(
            new GetBestSellersQuery(null, null), CancellationToken.None);

        var row = Assert.Single(best);
        Assert.Equal(7, row.QuantitySold);
        Assert.Equal(80m, row.Revenue);
    }

    [Fact]
    public async Task The_dashboard_card_reports_the_ledger_and_drops_the_utang_payment_row()
    {
        await SeedSettingsAsync();
        var rosa = await SeedSukiAsync("Aling Rosa");
        var tonyo = await SeedSukiAsync("Mang Tonyo");
        await SeedChargeAsync(rosa.Id, 200m);
        await SeedPaymentAsync(rosa.Id, 50m);
        await SeedChargeAsync(tonyo.Id, 80m);
        await SeedPaymentAsync(tonyo.Id, 80m);

        var summary = await new GetDashboardSummaryQueryHandler(
                _transactions, _items, _utang)
            .Handle(new GetDashboardSummaryQuery(), CancellationToken.None);

        Assert.Equal(150m, summary.Utang.TotalOutstanding);
        Assert.Equal(1, summary.Utang.SukiCount);
        Assert.Equal(130m, summary.Utang.CollectedThisWeek);
        var top = Assert.Single(summary.Utang.Top);
        Assert.Equal("Aling Rosa", top.Name);
        Assert.DoesNotContain(summary.PaymentsToday, p => p.Method == "Utang");
    }

    [Fact]
    public async Task Summary_sums_charges_and_payments_and_excludes_voided()
    {
        var suki = await SeedSukiAsync();
        await SeedChargeAsync(suki.Id, 100m);
        await SeedChargeAsync(suki.Id, 50m, voided: true);
        await SeedPaymentAsync(suki.Id, 30m);
        await SeedPaymentAsync(suki.Id, 10m, voided: true);

        var handler = new GetUtangSummaryQueryHandler(_utang);
        var result = await handler.Handle(
            new GetUtangSummaryQuery(null, null), default);

        Assert.Equal(100m, result.TotalCharged);
        Assert.Equal(30m, result.TotalPaid);
        Assert.Equal("Aling Rosa", result.TopSukiName);
        Assert.Equal(100m, result.TopSukiCharged);
    }

    [Fact]
    public async Task Summary_filters_entries_outside_the_period()
    {
        var suki = await SeedSukiAsync();
        var from = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 8, 31, 23, 59, 59, DateTimeKind.Utc);
        await SeedChargeAsync(suki.Id, 100m,
            createdAt: new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc));
        await SeedChargeAsync(suki.Id, 40m,
            createdAt: new DateTime(2026, 7, 31, 12, 0, 0, DateTimeKind.Utc));
        await SeedPaymentAsync(suki.Id, 20m,
            createdAt: new DateTime(2026, 8, 15, 12, 0, 0, DateTimeKind.Utc));
        await SeedPaymentAsync(suki.Id, 5m,
            createdAt: new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc));

        var handler = new GetUtangSummaryQueryHandler(_utang);
        var result = await handler.Handle(
            new GetUtangSummaryQuery(from, to), default);

        Assert.Equal(100m, result.TotalCharged);
        Assert.Equal(20m, result.TotalPaid);
    }

    [Fact]
    public async Task Summary_picks_the_suki_with_the_largest_charges_in_period()
    {
        var rosa = await SeedSukiAsync();
        var daisy = await SeedSukiAsync("Daisy Jane");
        var from = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        await SeedChargeAsync(rosa.Id, 500m,
            createdAt: new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc));
        await SeedChargeAsync(rosa.Id, 100m,
            createdAt: new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc));
        await SeedChargeAsync(daisy.Id, 200m,
            createdAt: new DateTime(2026, 8, 12, 12, 0, 0, DateTimeKind.Utc));

        var handler = new GetUtangSummaryQueryHandler(_utang);
        var result = await handler.Handle(
            new GetUtangSummaryQuery(from, null), default);

        Assert.Equal("Daisy Jane", result.TopSukiName);
        Assert.Equal(200m, result.TopSukiCharged);
        Assert.Equal(300m, result.TotalCharged);
    }

    [Fact]
    public async Task Summary_returns_zeros_and_no_top_suki_when_period_has_no_entries()
    {
        await SeedSukiAsync();

        var handler = new GetUtangSummaryQueryHandler(_utang);
        var result = await handler.Handle(
            new GetUtangSummaryQuery(null, null), default);

        Assert.Equal(0m, result.TotalCharged);
        Assert.Equal(0m, result.TotalPaid);
        Assert.Null(result.TopSukiName);
        Assert.Equal(0m, result.TopSukiCharged);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }
}
