using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Application.Utang.Queries.GetSukiLedgerEntries;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Domain.Exceptions;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;
using POS.Infrastructure.Tests.Fakes;
using Xunit;

namespace POS.Infrastructure.Tests;

/// <summary>Paged suki ledger entries with the server-side running balance.</summary>
public class LedgerPagingTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _ctx;
    private readonly UtangRepository _utang;
    private readonly FakeCurrentUser _user = new();
    private readonly Shift _shift;
    private readonly Suki _suki;
    private int _seq;

    private static readonly DateTime T0 = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);

    public LedgerPagingTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _ctx = new AppDbContext(options);
        _ctx.Database.EnsureCreated();
        PaymentMethodSeeder.Seed(_ctx);

        _utang = new UtangRepository(_ctx);

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
        _suki = new Suki { Name = "Aling Rosa", CreatedBy = _user.Id };
        _ctx.Sukis.Add(_suki);
        _ctx.SaveChanges();
    }

    private async Task<Invoice> ChargeAsync(decimal total, DateTime at, bool voided = false, decimal markup = 0m)
    {
        var invoice = new Invoice
        {
            InvoiceNumber = $"INV{++_seq:D4}",
            SukiId = _suki.Id,
            ShiftId = _shift.Id,
            Subtotal = total,
            Total = total,
            MarkupTotal = markup,
            IsVoided = voided,
            CreatedBy = _user.Id,
            CreatedAt = at
        };
        _ctx.Invoices.Add(invoice);
        await _ctx.SaveChangesAsync();
        return invoice;
    }

    private async Task<Payment> PayAsync(decimal amount, DateTime at, bool voided = false, string? note = null)
    {
        var payment = new Payment
        {
            SukiId = _suki.Id,
            Amount = amount,
            Note = note,
            IsVoided = voided,
            CreatedBy = _user.Id,
            CreatedAt = at
        };
        _ctx.Payments.Add(payment);
        await _ctx.SaveChangesAsync();
        return payment;
    }

    private GetSukiLedgerEntriesQueryHandler Handler() => new(_utang);

    [Fact]
    public async Task Ledger_entries_page_newest_first()
    {
        Invoice? latest = null;
        for (var i = 1; i <= 25; i++)
            latest = await ChargeAsync(10m, T0.AddMinutes(i));

        var page = await Handler().Handle(
            new GetSukiLedgerEntriesQuery(_suki.Id, null, null), CancellationToken.None);

        Assert.Equal(20, page.Items.Count);
        Assert.Equal(25, page.TotalCount);
        Assert.Equal(latest!.Id, page.Items[0].Id);
        Assert.Equal(latest.InvoiceNumber, page.Items[0].InvoiceNumber);
        Assert.Equal(250m, page.Items[0].RunningBalance);
    }

    [Fact]
    public async Task Running_balance_is_chronological_across_pages()
    {
        await ChargeAsync(10m, T0);
        await ChargeAsync(20m, T0.AddMinutes(1));
        await ChargeAsync(30m, T0.AddMinutes(2));
        await PayAsync(15m, T0.AddMinutes(3));

        var first = await Handler().Handle(
            new GetSukiLedgerEntriesQuery(_suki.Id, 1, 2), CancellationToken.None);
        var second = await Handler().Handle(
            new GetSukiLedgerEntriesQuery(_suki.Id, 2, 2), CancellationToken.None);

        Assert.Equal(4, first.TotalCount);
        Assert.Equal(new[] { "Payment", "Charge" }, first.Items.Select(e => e.Type).ToArray());
        Assert.Equal(new decimal?[] { 45m, 60m }, first.Items.Select(e => e.RunningBalance).ToArray());
        Assert.Equal(new decimal?[] { 30m, 10m }, second.Items.Select(e => e.RunningBalance).ToArray());
    }

    [Fact]
    public async Task Voided_entries_carry_no_running_balance_and_do_not_move_it()
    {
        await ChargeAsync(10m, T0);
        await ChargeAsync(99m, T0.AddMinutes(1), voided: true);
        await PayAsync(4m, T0.AddMinutes(2));

        var page = await Handler().Handle(
            new GetSukiLedgerEntriesQuery(_suki.Id, null, null), CancellationToken.None);

        Assert.Equal(new decimal?[] { 6m, null, 10m }, page.Items.Select(e => e.RunningBalance).ToArray());
        Assert.True(page.Items[1].IsVoided);
        Assert.Equal("Payment received", page.Items[0].Note);
    }

    [Fact]
    public async Task Entries_for_unknown_suki_throw_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            Handler().Handle(new GetSukiLedgerEntriesQuery(Guid.NewGuid(), null, null), CancellationToken.None));
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }
}
