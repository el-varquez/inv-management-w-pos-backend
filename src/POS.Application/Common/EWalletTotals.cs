using POS.Domain.Entities;
using POS.Domain.Enums;

namespace POS.Application.Common;

public record EWalletTotalsResult(
    int CashInCount,
    decimal CashIn,
    int CashOutCount,
    decimal CashOut,
    decimal WalletNet,
    decimal DrawerNet);

public static class EWalletTotals
{
    public static EWalletTotalsResult Of(IEnumerable<EWalletTransaction> transactions)
    {
        var live = transactions.Where(t => !t.IsVoided).ToList();
        var cashIn = live.Where(t => t.Direction == EWalletDirection.CashIn).ToList();
        var cashOut = live.Where(t => t.Direction == EWalletDirection.CashOut).ToList();

        return new EWalletTotalsResult(
            cashIn.Count,
            cashIn.Sum(t => t.Principal),
            cashOut.Count,
            cashOut.Sum(t => t.Principal),
            live.Sum(t => t.WalletDelta),
            live.Sum(t => t.DrawerDelta));
    }
}
