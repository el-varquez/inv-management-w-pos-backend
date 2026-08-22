using POS.Domain.Entities;

namespace POS.Application.Common;

public record UtangTotalsResult(
    int ChargeCount, decimal Charged, decimal Markup, decimal Collections);

public static class UtangTotals
{
    public static UtangTotalsResult Of(
        IEnumerable<UtangCharge> charges, IEnumerable<UtangPayment> payments)
    {
        var liveCharges = charges.Where(c => !c.IsVoided).ToList();

        return new UtangTotalsResult(
            liveCharges.Count,
            liveCharges.Sum(c => c.Amount),
            liveCharges.Sum(c => c.Markup),
            payments.Where(p => !p.IsVoided).Sum(p => p.Amount));
    }
}
