using MediatR;
using POS.Application.Common.Models;

namespace POS.Application.Shifts.Queries.GetShifts;

public record GetShiftsQuery(int? Page, int? PageSize) : IRequest<PagedResult<ShiftSummaryDto>>;

public record ShiftSummaryDto(
    Guid Id,
    int Number,
    bool IsClosed,
    DateTime OpenedAt,
    DateTime? ClosedAt,
    decimal StartingCash,
    decimal? NetSales,
    decimal? ExpectedCash,
    decimal? CountedCash,
    decimal? CashVariance);
