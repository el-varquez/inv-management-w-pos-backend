using POS.Domain.Entities;
using POS.Domain.Enums;

namespace POS.Application.Common;

public static class PaidSales
{
    /// Paid sales netting convention (matches the sales report):
    /// non-refund totals minus |refund totals|. Utang is never in here.
    public static decimal Net(IEnumerable<Transaction> transactions)
    {
        var paid = Paid(transactions);
        var sales = paid.Where(t => t.RefundedFromId == null).Sum(t => t.Total);
        var refunds = Math.Abs(
            paid.Where(t => t.RefundedFromId != null).Sum(t => t.Total));
        return sales - refunds;
    }

    public static int Count(IEnumerable<Transaction> transactions)
        => Paid(transactions).Count(t => t.RefundedFromId == null);

    public static decimal Refunds(IEnumerable<Transaction> transactions)
        => Math.Abs(Paid(transactions)
            .Where(t => t.RefundedFromId != null).Sum(t => t.Total));

    public static int RefundCount(IEnumerable<Transaction> transactions)
        => Paid(transactions).Count(t => t.RefundedFromId != null);

    private static IEnumerable<Transaction> Paid(
        IEnumerable<Transaction> transactions)
        => transactions.Where(t => t.PaymentType != PaymentType.Utang)
            .ToList();
}
