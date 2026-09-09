using POS.Application.Common;
using POS.Domain.Entities;
using Xunit;

namespace POS.Infrastructure.Tests;

public class UtangReminderTests
{
    private static DateTime Noon(int month, int day)
        => new(2026, month, day, 12, 0, 0, DateTimeKind.Utc);

    private static Invoice InvoiceOn(int month, int day, decimal total, bool voided = false)
        => new() { Total = total, CreatedAt = Noon(month, day), IsVoided = voided };

    private static Payment PaymentOn(int month, int day, decimal amount, bool voided = false)
        => new() { Amount = amount, CreatedAt = Noon(month, day), IsVoided = voided };

    private static DateTime Today(int month, int day) => new(2026, month, day);

    [Fact]
    public void Six_days_after_the_first_utang_is_not_overdue()
    {
        var r = UtangReminder.Of(new[] { InvoiceOn(9, 1, 100m) }, Array.Empty<Payment>(), Today(9, 7), 7);

        Assert.Equal(Noon(9, 1), r.DebtSince);
        Assert.Null(r.LastPaidAt);
        Assert.Equal(6, r.DaysSincePayment);
        Assert.False(r.PaymentOverdue);
    }

    [Fact]
    public void Seven_days_after_the_first_utang_is_overdue()
    {
        var r = UtangReminder.Of(new[] { InvoiceOn(9, 1, 100m) }, Array.Empty<Payment>(), Today(9, 8), 7);

        Assert.Equal(7, r.DaysSincePayment);
        Assert.True(r.PaymentOverdue);
    }

    [Fact]
    public void A_partial_payment_moves_the_reference_date()
    {
        var invoices = new[] { InvoiceOn(9, 1, 100m) };
        var payments = new[] { PaymentOn(9, 9, 40m) };

        var onThe15th = UtangReminder.Of(invoices, payments, Today(9, 15), 7);
        var onThe16th = UtangReminder.Of(invoices, payments, Today(9, 16), 7);

        Assert.Equal(Noon(9, 1), onThe15th.DebtSince);
        Assert.Equal(Noon(9, 9), onThe15th.LastPaidAt);
        Assert.Equal(6, onThe15th.DaysSincePayment);
        Assert.False(onThe15th.PaymentOverdue);
        Assert.True(onThe16th.PaymentOverdue);
    }

    [Fact]
    public void Paying_in_full_starts_a_new_cycle_at_the_next_utang()
    {
        var invoices = new[] { InvoiceOn(9, 1, 100m), InvoiceOn(9, 10, 50m) };
        var payments = new[] { PaymentOn(9, 9, 100m) };

        var onThe16th = UtangReminder.Of(invoices, payments, Today(9, 16), 7);
        var onThe17th = UtangReminder.Of(invoices, payments, Today(9, 17), 7);

        Assert.Equal(Noon(9, 10), onThe16th.DebtSince);
        Assert.Null(onThe16th.LastPaidAt);
        Assert.Equal(6, onThe16th.DaysSincePayment);
        Assert.False(onThe16th.PaymentOverdue);
        Assert.True(onThe17th.PaymentOverdue);
    }

    [Fact]
    public void A_credit_balance_has_no_debt_cycle()
    {
        var r = UtangReminder.Of(
            new[] { InvoiceOn(9, 1, 100m) }, new[] { PaymentOn(9, 2, 120m) }, Today(9, 30), 7);

        Assert.Null(r.DebtSince);
        Assert.Null(r.LastPaidAt);
        Assert.Null(r.DaysSincePayment);
        Assert.False(r.PaymentOverdue);
    }

    [Fact]
    public void Voided_entries_are_ignored()
    {
        var invoices = new[] { InvoiceOn(9, 1, 100m, voided: true), InvoiceOn(9, 5, 30m) };
        var payments = new[] { PaymentOn(9, 6, 30m, voided: true) };

        var r = UtangReminder.Of(invoices, payments, Today(9, 12), 7);

        Assert.Equal(Noon(9, 5), r.DebtSince);
        Assert.Null(r.LastPaidAt);
        Assert.True(r.PaymentOverdue);
    }

    [Fact]
    public void The_reminder_days_setting_is_respected()
    {
        var invoices = new[] { InvoiceOn(9, 1, 100m) };

        Assert.True(UtangReminder.Of(invoices, Array.Empty<Payment>(), Today(9, 4), 3).PaymentOverdue);
        Assert.False(UtangReminder.Of(invoices, Array.Empty<Payment>(), Today(9, 4), 30).PaymentOverdue);
    }

    [Fact]
    public void An_empty_ledger_has_no_cycle()
    {
        var r = UtangReminder.Of(Array.Empty<Invoice>(), Array.Empty<Payment>(), Today(9, 4), 7);

        Assert.Null(r.DebtSince);
        Assert.False(r.PaymentOverdue);
    }
}
