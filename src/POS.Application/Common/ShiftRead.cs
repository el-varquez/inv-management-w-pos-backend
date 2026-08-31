using POS.Domain.Entities;
using POS.Domain.Enums;

namespace POS.Application.Common;

public record DrawerMovementDto(
    Guid Id,
    decimal Amount,
    string Note,
    bool IsVoided,
    DateTime CreatedAt);

public record MethodSalesDto(Guid PaymentMethodId, string Name, decimal Amount);

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
    IList<MethodSalesDto> MethodSales,
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
    IList<DrawerMovementDto> Movements);

public static class ShiftRead
{
    public static ShiftReadDto Build(
        Shift shift,
        IList<Transaction> transactions,
        IList<CashDrawerMovement> movements,
        IList<UtangCharge> utangCharges,
        IList<UtangPayment> utangPayments,
        IList<PaymentMethod> methods)
    {
        var movementDtos = movements
            .Select(m => new DrawerMovementDto(m.Id, m.Amount, m.Note, m.IsVoided, m.CreatedAt))
            .ToList();

        if (shift.Status == ShiftStatus.Closed && shift.Snapshot is not null)
        {
            var s = shift.Snapshot;
            var frozenMethodSales = shift.MethodSales
                .Select(m => new MethodSalesDto(m.PaymentMethodId, m.MethodName, m.Amount))
                .ToList();
            return new ShiftReadDto(
                shift.Id, shift.Number, true,
                shift.OpenedAt, shift.ClosedAt,
                shift.StartingCash, shift.StartingCashOriginal,
                shift.StartingCashCorrectionReason,
                s.NetSales, s.TransactionCount,
                s.Refunds, s.RefundCount,
                frozenMethodSales,
                s.UtangChargedCount, s.UtangCharged, s.UtangMarkup, s.UtangCollections,
                s.DrawerMovementsNet, s.ExpectedCash,
                s.CountedCash, s.CountedCashOriginal, s.CorrectionReason, s.CashVariance,
                movementDtos);
        }

        var utang = UtangTotals.Of(utangCharges, utangPayments);
        var movementsNet = movements.Where(m => !m.IsVoided).Sum(m => m.Amount);

        var methodSales = methods
            .Where(m => m.Type == PaymentMethodType.Sales)
            .Where(m => m.IsActive || transactions.Any(t => t.PaymentMethodId == m.Id))
            .Select(m => new MethodSalesDto(m.Id, m.Name,
                PaidSales.Net(transactions.Where(t => t.PaymentMethodId == m.Id))))
            .ToList();
        var cashSales = methodSales
            .FirstOrDefault(m => m.PaymentMethodId == PaymentMethodIds.Cash)?.Amount ?? 0m;
        var eWalletSales = methodSales
            .FirstOrDefault(m => m.PaymentMethodId == PaymentMethodIds.EWallet)?.Amount ?? 0m;

        var expectedCash = shift.StartingCash + cashSales + eWalletSales
            + movementsNet + utang.Collections;

        return new ShiftReadDto(
            shift.Id, shift.Number, false,
            shift.OpenedAt, null,
            shift.StartingCash, shift.StartingCashOriginal,
            shift.StartingCashCorrectionReason,
            PaidSales.Net(transactions), PaidSales.Count(transactions),
            PaidSales.Refunds(transactions), PaidSales.RefundCount(transactions),
            methodSales,
            utang.ChargeCount, utang.Charged, utang.Markup, utang.Collections,
            movementsNet, expectedCash,
            null, null, null, null,
            movementDtos);
    }
}
