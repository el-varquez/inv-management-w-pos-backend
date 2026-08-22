using POS.Domain.Entities;
using POS.Domain.Enums;

namespace POS.Application.Common;

public record UtangTotalsResult(
    int ChargeCount, decimal Charged, decimal Markup, decimal Collections);

public static class UtangTotals
{
    public static UtangTotalsResult Of(IEnumerable<UtangEntry> entries)
    {
        var live = entries.Where(e => !e.IsVoided).ToList();
        var charges = live.Where(e => e.Type == UtangEntryType.Charge).ToList();

        return new UtangTotalsResult(
            charges.Count,
            charges.Sum(e => e.Amount),
            charges.Sum(e => e.Markup),
            live.Where(e => e.Type == UtangEntryType.Payment).Sum(e => e.Amount));
    }
}
