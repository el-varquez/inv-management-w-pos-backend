using POS.Domain.Entities;

namespace POS.Application.Common;

public static class PaidSales
{
    /// Paid sales netting convention (matches the sales report):
    /// non-refund totals minus |refund totals|. Utang is never in here.
    public static decimal Net(IEnumerable<Transaction> transactions)
    {
        var sales = transactions.Where(t => t.RefundedFromId == null).Sum(t => t.Total);
        var refunds = Math.Abs(
            transactions.Where(t => t.RefundedFromId != null).Sum(t => t.Total));
        return sales - refunds;
    }

    public static int Count(IEnumerable<Transaction> transactions)
        => transactions.Count(t => t.RefundedFromId == null);

    public static decimal Refunds(IEnumerable<Transaction> transactions)
        => Math.Abs(transactions.Where(t => t.RefundedFromId != null).Sum(t => t.Total));

    public static int RefundCount(IEnumerable<Transaction> transactions)
        => transactions.Count(t => t.RefundedFromId != null);
}
