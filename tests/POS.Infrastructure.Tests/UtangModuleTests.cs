using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Application.Dashboard.Queries.GetDashboardSummary;
using POS.Application.Invoices.Commands.CreateInvoice;
using POS.Application.Items.Queries.GetPopularItems;
using POS.Application.Reports.Queries.GetBestSellers;
using POS.Application.Sales.Commands.CreateSale;
using POS.Application.Sales.Commands.ProcessRefund;
using POS.Application.Sales.Queries.GetSalesSummary;
using POS.Application.Shifts.Commands.CloseShift;
using POS.Application.Shifts.Queries.GetShiftRead;
using POS.Application.Utang.Commands.CollectUtangPayment;
using POS.Application.Utang.Commands.CreateSuki;
using POS.Application.Utang.Commands.DeleteSuki;
using POS.Application.Utang.Commands.EditUtangPayment;
using POS.Application.Utang.Commands.UpdateSuki;
using POS.Application.Utang.Commands.VoidUtangPayment;
using POS.Application.Utang.Queries.GetSukiLedger;
using POS.Application.Utang.Queries.GetSukis;
using POS.Application.Utang.Queries.GetUtangSummary;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Domain.Exceptions;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;
using POS.Infrastructure.Tests.Fakes;
using Xunit;

namespace POS.Infrastructure.Tests;

public class UtangModuleTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _ctx;
    private readonly ItemRepository _items;
    private readonly CompositeItemRepository _composites;
    private readonly SaleRepository _sales;
    private readonly InvoiceRepository _invoices;
    private readonly ShiftRepository _shifts;
    private readonly StoreSettingsRepository _settings;
    private readonly UtangRepository _utang;
    private readonly PaymentMethodRepository _paymentMethods;
    private readonly UnitOfWork _uow;
    private readonly FakeCurrentUser _user = new();
    private readonly Guid _categoryId = Guid.NewGuid();
    private readonly Shift _shift;
    private int _codeSeq;

    public UtangModuleTests()
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
        _invoices = new InvoiceRepository(_ctx);
        _shifts = new ShiftRepository(_ctx);
        _settings = new StoreSettingsRepository(_ctx);
        _utang = new UtangRepository(_ctx);
        _paymentMethods = new PaymentMethodRepository(_ctx);
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
        _ctx.StoreSettings.Add(new StoreSettings
        {
            StoreName = "Test Store",
            DefaultUtangMarkup = 1m,
            AcceptUtang = true,
            UtangReminderDays = 7
        });
        _ctx.SaveChanges();
    }

    private CreateInvoiceCommandHandler InvoiceHandler()
        => new(_items, _composites, _invoices, _utang, _shifts, _settings,
            new FakeInvoiceNumberGenerator(), _uow, _user);

    private CreateSaleCommandHandler SaleHandler()
        => new(_items, _sales, new FakeReceiptNumberGenerator(), _uow, _user,
            _composites, _shifts, _paymentMethods);

    private ProcessRefundCommandHandler RefundHandler()
        => new(_sales, new FakeReceiptNumberGenerator(), _uow, _user, _shifts);

    private CollectUtangPaymentCommandHandler CollectHandler()
        => new(_utang, _settings, _uow, _user);

    private GetShiftReadQueryHandler ReadHandler()
        => new(_shifts, _sales, _paymentMethods);

    private CloseShiftCommandHandler CloseHandler()
        => new(_shifts, _sales, _uow, _user, _paymentMethods);

    private GetSukisQueryHandler SukisHandler() => new(_utang, _settings);

    private GetSukiLedgerQueryHandler LedgerHandler() => new(_utang, _invoices, _settings);

    private GetUtangSummaryQueryHandler SummaryHandler() => new(_utang, _invoices);

    private async Task<Suki> SeedSukiAsync(string name = "Aling Rosa", string? phone = null)
    {
        var suki = new Suki { Name = name, Phone = phone, CreatedBy = _user.Id };
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
            ItemCode = $"U{++_codeSeq:D4}",
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

    private async Task<CreateInvoiceResult> InvoiceAsync(Suki suki, Item item, int qty)
        => await InvoiceHandler().Handle(
            new CreateInvoiceCommand(
                new List<InvoiceLineInput> { new(item.Id, qty, 0m) }, 0m, suki.Id),
            default);

    private async Task<Invoice> SeedInvoiceAsync(
        Suki suki, decimal total, DateTime createdAt, bool voided = false, decimal markup = 0m)
    {
        var invoice = new Invoice
        {
            InvoiceNumber = $"INV-SEED-{Guid.NewGuid():N}"[..20],
            SukiId = suki.Id,
            ShiftId = _shift.Id,
            Subtotal = total,
            MarkupTotal = markup,
            Total = total,
            IsVoided = voided,
            CreatedBy = _user.Id,
            CreatedAt = createdAt
        };
        _ctx.Invoices.Add(invoice);
        await _ctx.SaveChangesAsync();
        return invoice;
    }

    private async Task<Payment> SeedPaymentAsync(
        Suki suki, decimal amount, DateTime createdAt, bool voided = false, string? note = null)
    {
        var payment = new Payment
        {
            SukiId = suki.Id,
            Amount = amount,
            Note = note,
            IsVoided = voided,
            CreatedBy = _user.Id,
            CreatedAt = createdAt
        };
        _ctx.Payments.Add(payment);
        await _ctx.SaveChangesAsync();
        return payment;
    }

    private async Task SaleAsync(Item item, int qty)
        => await SaleHandler().Handle(
            new CreateSaleCommand(
                new List<CartItemInput> { new(item.Id, qty, 0m) },
                0m, PaymentMethodIds.Cash, item.SellingPrice * qty),
            default);

    private async Task SetAcceptUtangAsync(bool on)
    {
        var settings = await _ctx.StoreSettings.SingleAsync();
        settings.AcceptUtang = on;
        await _ctx.SaveChangesAsync();
    }

    private static DateTime DaysAgo(int days) => DateTime.UtcNow.AddDays(-days);

    [Fact]
    public async Task Creating_a_suki_trims_and_persists()
    {
        var handler = new CreateSukiCommandHandler(_utang, _settings, _uow, _user);

        var dto = await handler.Handle(new CreateSukiCommand("  Mang Ben ", " 0917 "), default);

        Assert.Equal("Mang Ben", dto.Name);
        Assert.Equal("0917", dto.Phone);
        Assert.Equal(0m, dto.Balance);
        Assert.False(dto.PaymentOverdue);
        Assert.Equal(1, await _ctx.Sukis.CountAsync());
    }

    [Fact]
    public async Task Creating_a_suki_is_refused_while_utang_is_off()
    {
        await SetAcceptUtangAsync(false);
        var handler = new CreateSukiCommandHandler(_utang, _settings, _uow, _user);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(new CreateSukiCommand("Mang Ben", null), default));

        Assert.Equal("Utang is turned off — turn it on in web admin Settings.", ex.Message);
    }

    [Fact]
    public async Task Editing_a_suki_trims_and_persists()
    {
        var suki = await SeedSukiAsync();
        var handler = new UpdateSukiCommandHandler(_utang, _settings, _uow);

        await handler.Handle(new UpdateSukiCommand(suki.Id, " Rosa Cruz ", " 0918 "), default);

        var saved = await _ctx.Sukis.SingleAsync();
        Assert.Equal("Rosa Cruz", saved.Name);
        Assert.Equal("0918", saved.Phone);
    }

    [Fact]
    public async Task Editing_a_suki_clears_a_blank_phone()
    {
        var suki = await SeedSukiAsync(phone: "0917");
        var handler = new UpdateSukiCommandHandler(_utang, _settings, _uow);

        await handler.Handle(new UpdateSukiCommand(suki.Id, "Aling Rosa", "  "), default);

        Assert.Null((await _ctx.Sukis.SingleAsync()).Phone);
    }

    [Fact]
    public async Task Editing_an_unknown_suki_is_refused()
    {
        var handler = new UpdateSukiCommandHandler(_utang, _settings, _uow);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new UpdateSukiCommand(Guid.NewGuid(), "X", null), default));
    }

    [Fact]
    public void A_suki_without_a_name_is_refused()
    {
        var result = new CreateSukiCommandValidator().Validate(new CreateSukiCommand("  ", null));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task A_suki_with_no_history_is_deleted()
    {
        var suki = await SeedSukiAsync();

        await new DeleteSukiCommandHandler(_utang, _settings, _uow)
            .Handle(new DeleteSukiCommand(suki.Id), default);

        Assert.Equal(0, await _ctx.Sukis.CountAsync());
    }

    [Fact]
    public async Task A_suki_with_an_invoice_is_not_deleted()
    {
        var suki = await SeedSukiAsync("Mang Ben");
        await SeedInvoiceAsync(suki, 50m, DaysAgo(1));

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => new DeleteSukiCommandHandler(_utang, _settings, _uow)
                .Handle(new DeleteSukiCommand(suki.Id), default));

        Assert.Equal("Mang Ben has ledger history and can't be deleted.", ex.Message);
    }

    [Fact]
    public async Task A_suki_with_only_a_payment_is_not_deleted()
    {
        var suki = await SeedSukiAsync();
        await SeedPaymentAsync(suki, 10m, DaysAgo(1));

        await Assert.ThrowsAsync<DomainException>(
            () => new DeleteSukiCommandHandler(_utang, _settings, _uow)
                .Handle(new DeleteSukiCommand(suki.Id), default));
    }

    [Fact]
    public async Task Deleting_an_unknown_suki_is_refused()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => new DeleteSukiCommandHandler(_utang, _settings, _uow)
                .Handle(new DeleteSukiCommand(Guid.NewGuid()), default));
    }

    [Fact]
    public async Task The_suki_list_computes_balances_and_filters_by_term()
    {
        var rosa = await SeedSukiAsync("Aling Rosa");
        var ben = await SeedSukiAsync("Mang Ben");
        await SeedInvoiceAsync(rosa, 100m, DaysAgo(3));
        await SeedPaymentAsync(rosa, 30m, DaysAgo(1));
        await SeedInvoiceAsync(ben, 20m, DaysAgo(2));

        var all = await SukisHandler().Handle(new GetSukisQuery(null, 1, 20), default);
        var filtered = await SukisHandler().Handle(new GetSukisQuery("ben", 1, 20), default);

        Assert.Equal(70m, all.Items.Single(s => s.Id == rosa.Id).Balance);
        Assert.Equal(20m, all.Items.Single(s => s.Id == ben.Id).Balance);
        Assert.Equal("Mang Ben", Assert.Single(filtered.Items).Name);
    }

    [Fact]
    public async Task The_suki_list_carries_the_reminder_fields()
    {
        var suki = await SeedSukiAsync();
        await SeedInvoiceAsync(suki, 100m, DaysAgo(8));

        var row = Assert.Single((await SukisHandler().Handle(new GetSukisQuery(null, 1, 20), default)).Items);

        Assert.True(row.PaymentOverdue);
        Assert.Equal(8, row.DaysSincePayment);
        Assert.NotNull(row.DebtSince);
        Assert.Null(row.LastPaidAt);
    }

    [Fact]
    public async Task The_balance_excludes_voided_entries()
    {
        var suki = await SeedSukiAsync();
        await SeedInvoiceAsync(suki, 100m, DaysAgo(3));
        await SeedInvoiceAsync(suki, 999m, DaysAgo(2), voided: true);
        await SeedPaymentAsync(suki, 40m, DaysAgo(1));
        await SeedPaymentAsync(suki, 999m, DaysAgo(1), voided: true);

        Assert.Equal(60m, await _utang.GetBalanceAsync(suki.Id));
    }

    [Fact]
    public async Task The_ledger_lists_entries_chronologically_with_markup_earned()
    {
        var suki = await SeedSukiAsync();
        var first = await SeedInvoiceAsync(suki, 100m, DaysAgo(3), markup: 5m);
        await SeedPaymentAsync(suki, 30m, DaysAgo(2), note: "Partial");
        await SeedInvoiceAsync(suki, 50m, DaysAgo(1), markup: 2m);

        var ledger = await LedgerHandler().Handle(new GetSukiLedgerQuery(suki.Id), default);

        Assert.Equal(120m, ledger.Balance);
        Assert.Equal(7m, ledger.MarkupEarned);
        Assert.Equal(new[] { "Charge", "Payment", "Charge" }, ledger.Entries.Select(e => e.Type).ToArray());
        Assert.Equal(first.InvoiceNumber, ledger.Entries[0].InvoiceNumber);
        Assert.Equal(first.Id, ledger.Entries[0].InvoiceId);
        Assert.Equal(5m, ledger.Entries[0].Markup);
        Assert.Equal("Partial", ledger.Entries[1].Note);
        Assert.NotNull(ledger.DebtSince);
    }

    [Fact]
    public async Task Collecting_writes_a_payment_and_lowers_the_balance()
    {
        var suki = await SeedSukiAsync();
        await SeedInvoiceAsync(suki, 100m, DaysAgo(1));

        var id = await CollectHandler().Handle(
            new CollectUtangPaymentCommand(suki.Id, 40m, null), default);

        var payment = await _ctx.Payments.SingleAsync(p => p.Id == id);
        Assert.Equal(40m, payment.Amount);
        Assert.Equal(_user.Id, payment.CreatedBy);
        Assert.Equal(60m, await _utang.GetBalanceAsync(suki.Id));
    }

    [Fact]
    public async Task Collecting_needs_no_open_shift_and_writes_no_movement()
    {
        var suki = await SeedSukiAsync();
        await SeedInvoiceAsync(suki, 100m, DaysAgo(1));
        _shift.Status = ShiftStatus.Closed;
        _shift.ClosedAt = DateTime.UtcNow;
        await _ctx.SaveChangesAsync();

        await CollectHandler().Handle(new CollectUtangPaymentCommand(suki.Id, 40m, null), default);

        Assert.Equal(1, await _ctx.Payments.CountAsync());
        Assert.Equal(0, await _ctx.CashDrawerMovements.CountAsync());
    }

    [Fact]
    public async Task Collecting_more_than_the_balance_is_refused()
    {
        var suki = await SeedSukiAsync("Aling Rosa");
        await SeedInvoiceAsync(suki, 100m, DaysAgo(1));

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => CollectHandler().Handle(new CollectUtangPaymentCommand(suki.Id, 100.01m, null), default));

        Assert.Equal("That's more than Aling Rosa owes — the balance is ₱100.00.", ex.Message);
    }

    [Fact]
    public async Task A_collection_note_reaches_the_ledger()
    {
        var suki = await SeedSukiAsync();
        await SeedInvoiceAsync(suki, 100m, DaysAgo(1));

        await CollectHandler().Handle(new CollectUtangPaymentCommand(suki.Id, 10m, "  Bayad kay Ben  "), default);

        var ledger = await LedgerHandler().Handle(new GetSukiLedgerQuery(suki.Id), default);
        Assert.Equal("Bayad kay Ben", ledger.Entries.Single(e => e.Type == "Payment").Note);
    }

    [Fact]
    public async Task A_blank_collection_note_falls_back_to_payment_received()
    {
        var suki = await SeedSukiAsync();
        await SeedInvoiceAsync(suki, 100m, DaysAgo(1));

        await CollectHandler().Handle(new CollectUtangPaymentCommand(suki.Id, 10m, "   "), default);

        var ledger = await LedgerHandler().Handle(new GetSukiLedgerQuery(suki.Id), default);
        Assert.Equal("Payment received", ledger.Entries.Single(e => e.Type == "Payment").Note);
    }

    [Fact]
    public async Task Payment_corrections_keep_the_first_original()
    {
        var suki = await SeedSukiAsync();
        var payment = await SeedPaymentAsync(suki, 40m, DaysAgo(1));
        var edit = new EditUtangPaymentCommandHandler(_utang, _settings, _uow, _user);

        await edit.Handle(new EditUtangPaymentCommand(payment.Id, 35m), default);
        await edit.Handle(new EditUtangPaymentCommand(payment.Id, 30m), default);

        var saved = await _ctx.Payments.SingleAsync();
        Assert.Equal(30m, saved.Amount);
        Assert.Equal(40m, saved.EditedFrom);
    }

    [Fact]
    public async Task A_voided_payment_cannot_be_edited()
    {
        var suki = await SeedSukiAsync();
        var payment = await SeedPaymentAsync(suki, 40m, DaysAgo(1), voided: true);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => new EditUtangPaymentCommandHandler(_utang, _settings, _uow, _user)
                .Handle(new EditUtangPaymentCommand(payment.Id, 10m), default));

        Assert.Equal("A voided payment can't be edited.", ex.Message);
    }

    [Fact]
    public async Task Voiding_a_payment_twice_is_refused()
    {
        var suki = await SeedSukiAsync();
        var payment = await SeedPaymentAsync(suki, 40m, DaysAgo(1));
        var voider = new VoidUtangPaymentCommandHandler(_utang, _settings, _uow, _user);
        await voider.Handle(new VoidUtangPaymentCommand(payment.Id), default);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => voider.Handle(new VoidUtangPaymentCommand(payment.Id), default));

        Assert.Equal("This payment is already voided.", ex.Message);
        Assert.Equal(_user.Id, (await _ctx.Payments.SingleAsync()).VoidedBy);
    }

    [Fact]
    public async Task Every_utang_write_is_refused_while_utang_is_off()
    {
        var suki = await SeedSukiAsync();
        var payment = await SeedPaymentAsync(suki, 10m, DaysAgo(1));
        await SetAcceptUtangAsync(false);
        const string off = "Utang is turned off — turn it on in web admin Settings.";

        var collect = await Assert.ThrowsAsync<DomainException>(
            () => CollectHandler().Handle(new CollectUtangPaymentCommand(suki.Id, 1m, null), default));
        var voidPayment = await Assert.ThrowsAsync<DomainException>(
            () => new VoidUtangPaymentCommandHandler(_utang, _settings, _uow, _user)
                .Handle(new VoidUtangPaymentCommand(payment.Id), default));
        var editPayment = await Assert.ThrowsAsync<DomainException>(
            () => new EditUtangPaymentCommandHandler(_utang, _settings, _uow, _user)
                .Handle(new EditUtangPaymentCommand(payment.Id, 5m), default));
        var update = await Assert.ThrowsAsync<DomainException>(
            () => new UpdateSukiCommandHandler(_utang, _settings, _uow)
                .Handle(new UpdateSukiCommand(suki.Id, "X", null), default));
        var delete = await Assert.ThrowsAsync<DomainException>(
            () => new DeleteSukiCommandHandler(_utang, _settings, _uow)
                .Handle(new DeleteSukiCommand(suki.Id), default));

        Assert.All(new[] { collect, voidPayment, editPayment, update, delete },
            ex => Assert.Equal(off, ex.Message));
        var stillReadable = await SukisHandler().Handle(new GetSukisQuery(null, 1, 20), default);
        Assert.Single(stillReadable.Items);
    }

    [Fact]
    public async Task The_live_x_read_carries_no_utang_and_a_collection_never_moves_expected_cash()
    {
        var suki = await SeedSukiAsync();
        var item = await SeedItemAsync(price: 10m);
        await SaleAsync(item, 2);
        await InvoiceAsync(suki, item, 3);

        var before = await ReadHandler().Handle(new GetShiftReadQuery(_shift.Id), default);
        await CollectHandler().Handle(new CollectUtangPaymentCommand(suki.Id, 15m, null), default);
        var after = await ReadHandler().Handle(new GetShiftReadQuery(_shift.Id), default);

        Assert.Equal(1020m, before.ExpectedCash);
        Assert.Equal(1020m, after.ExpectedCash);
        Assert.Equal(20m, after.NetSales);
        Assert.Equal(1, after.TransactionCount);
    }

    [Fact]
    public async Task Closing_the_shift_is_unchanged_by_invoices_and_collections()
    {
        var suki = await SeedSukiAsync();
        var item = await SeedItemAsync(price: 10m);
        await SaleAsync(item, 1);
        await InvoiceAsync(suki, item, 5);
        await CollectHandler().Handle(new CollectUtangPaymentCommand(suki.Id, 20m, null), default);

        await CloseHandler().Handle(new CloseShiftCommand(_shift.Id, 1010m), default);

        var read = await ReadHandler().Handle(new GetShiftReadQuery(_shift.Id), default);
        Assert.True(read.IsClosed);
        Assert.Equal(1010m, read.ExpectedCash);
        Assert.Equal(0m, read.CashVariance);
    }

    [Fact]
    public async Task A_refund_never_touches_the_ledger()
    {
        var suki = await SeedSukiAsync();
        var item = await SeedItemAsync(price: 10m);
        await SeedInvoiceAsync(suki, 100m, DaysAgo(1));
        var sale = await SaleHandler().Handle(
            new CreateSaleCommand(new List<CartItemInput> { new(item.Id, 1, 0m) }, 0m, PaymentMethodIds.Cash, 10m),
            default);

        await RefundHandler().Handle(new ProcessRefundCommand(sale.SaleId), default);

        Assert.Equal(100m, await _utang.GetBalanceAsync(suki.Id));
        Assert.Equal(0, await _ctx.Payments.CountAsync());
    }

    [Fact]
    public async Task The_sales_summary_reads_sales_only()
    {
        var suki = await SeedSukiAsync();
        var item = await SeedItemAsync(price: 10m);
        await SaleAsync(item, 2);
        await InvoiceAsync(suki, item, 3);

        var summary = await new GetSalesSummaryQueryHandler(_sales)
            .Handle(new GetSalesSummaryQuery(null, null), default);

        Assert.Equal(20m, summary.NetSales);
        Assert.Equal(1, summary.TransactionCount);
    }

    [Fact]
    public async Task Best_sellers_count_invoice_quantity_but_not_its_money()
    {
        var suki = await SeedSukiAsync();
        var item = await SeedItemAsync(price: 4m);
        await SaleAsync(item, 2);
        await InvoiceAsync(suki, item, 3);

        var rows = await new GetBestSellersQueryHandler(_sales, _invoices)
            .Handle(new GetBestSellersQuery(null, null), default);

        var row = Assert.Single(rows);
        Assert.Equal(5, row.QuantitySold);
        Assert.Equal(8m, row.Revenue);
        Assert.Equal(4m, row.Profit);
    }

    [Fact]
    public async Task Popular_items_count_invoice_quantity()
    {
        var suki = await SeedSukiAsync();
        var a = await SeedItemAsync("A");
        var b = await SeedItemAsync("B");
        await SaleAsync(a, 1);
        await InvoiceAsync(suki, b, 3);

        var tiles = await new GetPopularItemsQueryHandler(_items, _composites, _sales, _invoices)
            .Handle(new GetPopularItemsQuery(), default);

        Assert.Equal("B", tiles[0].Name);
        Assert.Equal(3, tiles[0].QuantitySold);
        Assert.Equal("A", tiles[1].Name);
    }

    [Fact]
    public async Task The_dashboard_card_reports_the_ledger()
    {
        var rosa = await SeedSukiAsync("Aling Rosa");
        var ben = await SeedSukiAsync("Mang Ben");
        await SeedInvoiceAsync(rosa, 100m, DaysAgo(3));
        await SeedPaymentAsync(rosa, 30m, DaysAgo(1));
        await SeedInvoiceAsync(ben, 200m, DaysAgo(2));
        await SeedPaymentAsync(ben, 50m, DaysAgo(10));

        var summary = await new GetDashboardSummaryQueryHandler(_sales, _items, _utang, _paymentMethods)
            .Handle(new GetDashboardSummaryQuery(), default);

        Assert.Equal(220m, summary.Utang.TotalOutstanding);
        Assert.Equal(2, summary.Utang.SukiCount);
        Assert.Equal(30m, summary.Utang.CollectedThisWeek);
        Assert.Equal("Mang Ben", summary.Utang.Top[0].Name);
    }

    [Fact]
    public async Task Summary_sums_invoices_and_payments_and_excludes_voided()
    {
        var suki = await SeedSukiAsync();
        await SeedInvoiceAsync(suki, 100m, DaysAgo(3));
        await SeedInvoiceAsync(suki, 999m, DaysAgo(3), voided: true);
        await SeedPaymentAsync(suki, 40m, DaysAgo(1));
        await SeedPaymentAsync(suki, 999m, DaysAgo(1), voided: true);

        var summary = await SummaryHandler().Handle(new GetUtangSummaryQuery(null, null), default);

        Assert.Equal(100m, summary.TotalCharged);
        Assert.Equal(40m, summary.TotalPaid);
    }

    [Fact]
    public async Task Summary_filters_entries_outside_the_period()
    {
        var suki = await SeedSukiAsync();
        await SeedInvoiceAsync(suki, 100m, DaysAgo(30));
        await SeedInvoiceAsync(suki, 50m, DaysAgo(1));
        await SeedPaymentAsync(suki, 20m, DaysAgo(30));
        await SeedPaymentAsync(suki, 10m, DaysAgo(1));

        var summary = await SummaryHandler()
            .Handle(new GetUtangSummaryQuery(DaysAgo(7), null), default);

        Assert.Equal(50m, summary.TotalCharged);
        Assert.Equal(10m, summary.TotalPaid);
    }

    [Fact]
    public async Task Summary_picks_the_suki_with_the_largest_invoices_in_period()
    {
        var rosa = await SeedSukiAsync("Aling Rosa");
        var ben = await SeedSukiAsync("Mang Ben");
        await SeedInvoiceAsync(rosa, 100m, DaysAgo(2));
        await SeedInvoiceAsync(ben, 80m, DaysAgo(2));
        await SeedInvoiceAsync(ben, 30m, DaysAgo(1));

        var summary = await SummaryHandler().Handle(new GetUtangSummaryQuery(null, null), default);

        Assert.Equal("Mang Ben", summary.TopSukiName);
        Assert.Equal(110m, summary.TopSukiCharged);
    }

    [Fact]
    public async Task Summary_returns_zeros_and_no_top_suki_when_period_has_no_entries()
    {
        var summary = await SummaryHandler().Handle(new GetUtangSummaryQuery(null, null), default);

        Assert.Equal(0m, summary.TotalCharged);
        Assert.Equal(0m, summary.TotalPaid);
        Assert.Null(summary.TopSukiName);
        Assert.Equal(0m, summary.TopSukiCharged);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }
}
