using POS.Domain.Entities;
using POS.Domain.Enums;

namespace POS.Application.Common;

public record DrawerMovementDto(
    Guid Id,
    decimal Amount,
    string Note,
    bool IsVoided,
    DateTime CreatedAt);

public record ShiftReadDto(
    Guid Id,
    int Number,
    bool IsClosed,
    DateTime OpenedAt,
    DateTime? ClosedAt,
    decimal StartingCash,
    decimal? StartingCashOriginal,
    string? StartingCashCorrectionReason,
    decimal NetSales,
    int TransactionCount,
    decimal Refunds,
    int RefundCount,
    decimal CashSales,
    decimal GcashSales,
    decimal MayaSales,
    int EWalletCashInCount,
    decimal EWalletCashIn,
    int EWalletCashOutCount,
    decimal EWalletCashOut,
    int UtangChargedCount,
    decimal UtangCharged,
    decimal UtangMarkup,
    decimal UtangCollections,
    decimal DrawerMovementsNet,
    decimal ExpectedCash,
    decimal? CountedCash,
    decimal? CountedCashOriginal,
    string? CorrectionReason,
    decimal? CashVariance,
    decimal? StartingEWalletBalance,
    decimal? ExpectedEWalletBalance,
    decimal? CountedEWalletBalance,
    decimal? EWalletVariance,
    IList<DrawerMovementDto> Movements);

public static class ShiftRead
{
    public static ShiftReadDto Build(
        Shift shift,
        IList<Transaction> transactions,
        IList<CashDrawerMovement> movements,
        IList<EWalletTransaction> eWalletTransactions,
        IList<UtangCharge> utangCharges,
        IList<UtangPayment> utangPayments)
    {
        var movementDtos = movements
            .Select(m => new DrawerMovementDto(m.Id, m.Amount, m.Note, m.IsVoided, m.CreatedAt))
            .ToList();

        if (shift.Status == ShiftStatus.Closed && shift.Snapshot is not null)
        {
            var s = shift.Snapshot;
            return new ShiftReadDto(
                shift.Id, shift.Number, true,
                shift.OpenedAt, shift.ClosedAt,
                shift.StartingCash, shift.StartingCashOriginal,
                shift.StartingCashCorrectionReason,
                s.NetSales, s.TransactionCount,
                s.Refunds, s.RefundCount,
                s.CashSales, s.GcashSales, s.MayaSales,
                s.EWalletCashInCount, s.EWalletCashIn,
                s.EWalletCashOutCount, s.EWalletCashOut,
                s.UtangChargedCount, s.UtangCharged, s.UtangMarkup, s.UtangCollections,
                s.DrawerMovementsNet, s.ExpectedCash,
                s.CountedCash, s.CountedCashOriginal, s.CorrectionReason, s.CashVariance,
                shift.StartingEWalletBalance, s.ExpectedEWalletBalance,
                s.CountedEWalletBalance, s.EWalletVariance,
                movementDtos);
        }

        var wallet = EWalletTotals.Of(eWalletTransactions);
        var utang = UtangTotals.Of(utangCharges, utangPayments);
        var movementsNet = movements.Where(m => !m.IsVoided).Sum(m => m.Amount);
        var cashSales = NetOf(transactions, PaymentType.Cash);
        var gcashSales = NetOf(transactions, PaymentType.Gcash);
        var expectedCash = shift.StartingCash + cashSales + movementsNet
            + wallet.DrawerNet + utang.Collections;
        var expectedWallet = shift.StartingEWalletBalance.HasValue
            ? shift.StartingEWalletBalance.Value + gcashSales + wallet.WalletNet
            : (decimal?)null;

        return new ShiftReadDto(
            shift.Id, shift.Number, false,
            shift.OpenedAt, null,
            shift.StartingCash, shift.StartingCashOriginal,
            shift.StartingCashCorrectionReason,
            PaidSales.Net(transactions), PaidSales.Count(transactions),
            PaidSales.Refunds(transactions), PaidSales.RefundCount(transactions),
            cashSales, gcashSales, NetOf(transactions, PaymentType.Maya),
            wallet.CashInCount, wallet.CashIn, wallet.CashOutCount, wallet.CashOut,
            utang.ChargeCount, utang.Charged, utang.Markup, utang.Collections,
            movementsNet, expectedCash,
            null, null, null, null,
            shift.StartingEWalletBalance, expectedWallet,
            null, null,
            movementDtos);
    }

    private static decimal NetOf(IList<Transaction> transactions, PaymentType method)
        => PaidSales.Net(transactions.Where(t => t.PaymentType == method));
}
