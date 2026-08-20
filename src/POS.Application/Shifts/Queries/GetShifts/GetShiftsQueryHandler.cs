using MediatR;
using POS.Application.Common;
using POS.Application.Common.Models;
using POS.Domain.Enums;
using POS.Domain.Interfaces;

namespace POS.Application.Shifts.Queries.GetShifts;

public class GetShiftsQueryHandler
    : IRequestHandler<GetShiftsQuery, PagedResult<ShiftSummaryDto>>
{
    private readonly IShiftRepository _shifts;

    public GetShiftsQueryHandler(IShiftRepository shifts) => _shifts = shifts;

    public async Task<PagedResult<ShiftSummaryDto>> Handle(
        GetShiftsQuery request, CancellationToken ct)
    {
        var (page, pageSize) = Pagination.Normalize(request.Page, request.PageSize);
        var (shifts, total) = await _shifts.GetPagedAsync(page, pageSize, ct);

        var dtos = shifts.Select(s => new ShiftSummaryDto(
            s.Id,
            s.Number,
            s.Status == ShiftStatus.Closed,
            s.OpenedAt,
            s.ClosedAt,
            s.StartingCash,
            s.Snapshot?.NetSales,
            s.Snapshot?.ExpectedCash,
            s.Snapshot?.CountedCash,
            s.Snapshot?.CashVariance
        )).ToList();

        return new PagedResult<ShiftSummaryDto>(dtos, page, pageSize, total);
    }
}
