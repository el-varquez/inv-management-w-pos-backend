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
        IList<Sale> sales,
        IList<CashDrawerMovement> movements,
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
                s.DrawerMovementsNet, s.ExpectedCash,
                s.CountedCash, s.CountedCashOriginal, s.CorrectionReason, s.CashVariance,
                movementDtos);
        }

        var movementsNet = movements.Where(m => !m.IsVoided).Sum(m => m.Amount);

        var methodSales = methods
            .Where(m => m.IsActive || sales.Any(t => t.PaymentMethodId == m.Id))
            .Select(m => new MethodSalesDto(m.Id, m.Name,
                PaidSales.Net(sales.Where(t => t.PaymentMethodId == m.Id))))
            .ToList();
        var expectedCash = shift.StartingCash + PaidSales.Net(sales) + movementsNet;

        return new ShiftReadDto(
            shift.Id, shift.Number, false,
            shift.OpenedAt, null,
            shift.StartingCash, shift.StartingCashOriginal,
            shift.StartingCashCorrectionReason,
            PaidSales.Net(sales), PaidSales.Count(sales),
            PaidSales.Refunds(sales), PaidSales.RefundCount(sales),
            methodSales,
            movementsNet, expectedCash,
            null, null, null, null,
            movementDtos);
    }
}
