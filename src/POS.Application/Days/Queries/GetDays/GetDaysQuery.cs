using MediatR;
using POS.Application.Common.Models;

namespace POS.Application.Days.Queries.GetDays;

public record GetDaysQuery(int? Page, int? PageSize) : IRequest<PagedResult<DaySummaryDto>>;

public record DaySummaryDto(
    Guid Id,
    int Number,
    bool IsClosed,
    bool ClosedLate,
    DateTime OpenedAt,
    DateTime? ClosedAt,
    decimal? NetSales,
    decimal? CountedCash,
    decimal? CashVariance,
    int ShiftCount);
