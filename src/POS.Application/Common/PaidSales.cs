using POS.Domain.Entities;

namespace POS.Application.Common;

public static class PaidSales
{
    /// Paid sales netting convention (matches the sales report):
    /// non-refund totals minus |refund totals|. Invoice methods are never in here.
    public static decimal Net(IEnumerable<Sale> sales)
    {
        var list = sales.ToList();
        var gross = list.Where(t => t.RefundedFromId == null).Sum(t => t.Total);
        var refunds = Math.Abs(list.Where(t => t.RefundedFromId != null).Sum(t => t.Total));
        return gross - refunds;
    }

    public static int Count(IEnumerable<Sale> sales)
        => sales.Count(t => t.RefundedFromId == null);

    public static decimal Refunds(IEnumerable<Sale> sales)
        => Math.Abs(sales.Where(t => t.RefundedFromId != null).Sum(t => t.Total));

    public static int RefundCount(IEnumerable<Sale> sales)
        => sales.Count(t => t.RefundedFromId != null);
}
