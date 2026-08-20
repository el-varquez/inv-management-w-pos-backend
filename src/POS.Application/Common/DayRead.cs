using POS.Application.Shifts.Queries.GetShifts;
using POS.Domain.Entities;
using POS.Domain.Enums;

namespace POS.Application.Common;

public record DayReadDto(
    Guid Id,
    int Number,
    bool IsClosed,
    bool ClosedLate,
    DateTime OpenedAt,
    DateTime? ClosedAt,
    decimal NetSales,
    int TransactionCount,
    decimal CashSales,
    decimal GcashSales,
    decimal MayaSales,
    decimal DrawerMovementsNet,
    decimal? CountedCash,
    decimal? CashVariance,
    decimal? CountedEWalletBalance,
    decimal? EWalletVariance,
    int ShiftCount,
    IList<ShiftSummaryDto> Shifts);

public static class DayRead
{
    public static DayReadDto Build(BusinessDay day, IList<ShiftReadDto> shiftReads)
    {
        var summaries = shiftReads
            .OrderBy(r => r.Number)
            .Select(r => new ShiftSummaryDto(
                r.Id, r.Number, r.IsClosed, r.OpenedAt, r.ClosedAt,
                r.StartingCash, r.NetSales, r.ExpectedCash, r.CountedCash, r.CashVariance))
            .ToList();

        if (day.Status == DayStatus.Closed && day.Snapshot is not null)
        {
            var s = day.Snapshot;
            return new DayReadDto(
                day.Id, day.Number, true, day.ClosedLate,
                day.OpenedAt, day.ClosedAt,
                s.NetSales, s.TransactionCount,
                s.CashSales, s.GcashSales, s.MayaSales,
                s.DrawerMovementsNet,
                s.CountedCash, s.CashVariance,
                s.CountedEWalletBalance, s.EWalletVariance,
                s.ShiftCount,
                summaries);
        }

        var closed = shiftReads
            .Where(r => r.IsClosed)
            .OrderBy(r => r.Number)
            .ToList();

        return new DayReadDto(
            day.Id, day.Number, day.Status == DayStatus.Closed, day.ClosedLate,
            day.OpenedAt, day.ClosedAt,
            shiftReads.Sum(r => r.NetSales),
            shiftReads.Sum(r => r.TransactionCount),
            shiftReads.Sum(r => r.CashSales),
            shiftReads.Sum(r => r.GcashSales),
            shiftReads.Sum(r => r.MayaSales),
            shiftReads.Sum(r => r.DrawerMovementsNet),
            closed.LastOrDefault()?.CountedCash,
            closed.Count > 0 ? closed.Sum(r => r.CashVariance ?? 0m) : null,
            closed.LastOrDefault()?.CountedEWalletBalance,
            closed.Any(r => r.EWalletVariance is not null)
                ? closed.Sum(r => r.EWalletVariance ?? 0m)
                : null,
            shiftReads.Count,
            summaries);
    }
}
