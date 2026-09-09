using POS.Domain.Entities;

namespace POS.Application.Common;

public record UtangReminderResult(
    DateTime? DebtSince,
    DateTime? LastPaidAt,
    int? DaysSincePayment,
    bool PaymentOverdue);

public static class UtangReminder
{
    public static UtangReminderResult Of(
        IEnumerable<Invoice> invoices,
        IEnumerable<Payment> payments,
        DateTime todayLocal,
        int reminderDays)
    {
        var entries = invoices
            .Where(i => !i.IsVoided)
            .Select(i => (At: i.CreatedAt, Delta: i.Total, IsPayment: false))
            .Concat(payments
                .Where(p => !p.IsVoided)
                .Select(p => (At: p.CreatedAt, Delta: -p.Amount, IsPayment: true)))
            .OrderBy(e => e.At)
            .ToList();

        var running = 0m;
        DateTime? debtSince = null;
        foreach (var entry in entries)
        {
            running += entry.Delta;
            if (running <= 0m)
            {
                debtSince = null;
                continue;
            }
            if (debtSince is null && !entry.IsPayment)
                debtSince = entry.At;
        }

        if (debtSince is null)
            return new UtangReminderResult(null, null, null, false);

        var lastPaidAt = entries
            .Where(e => e.IsPayment && e.At >= debtSince.Value)
            .Select(e => (DateTime?)e.At)
            .Max();
        var reference = lastPaidAt ?? debtSince.Value;
        var days = (todayLocal.Date - reference.ToLocalTime().Date).Days;

        return new UtangReminderResult(debtSince, lastPaidAt, days, days >= reminderDays);
    }
}
