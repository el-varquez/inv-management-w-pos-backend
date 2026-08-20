using MediatR;
using POS.Application.Common.Models;
using POS.Domain.Enums;
using POS.Domain.Interfaces;

namespace POS.Application.Days.Queries.GetDays;

public class GetDaysQueryHandler
    : IRequestHandler<GetDaysQuery, PagedResult<DaySummaryDto>>
{
    private readonly IBusinessDayRepository _days;

    public GetDaysQueryHandler(IBusinessDayRepository days) => _days = days;

    public async Task<PagedResult<DaySummaryDto>> Handle(
        GetDaysQuery request, CancellationToken ct)
    {
        var (page, pageSize) = Pagination.Normalize(request.Page, request.PageSize);
        var (days, total) = await _days.GetPagedAsync(page, pageSize, ct);

        var dtos = days.Select(d => new DaySummaryDto(
            d.Id,
            d.Number,
            d.Status == DayStatus.Closed,
            d.ClosedLate,
            d.OpenedAt,
            d.ClosedAt,
            d.Snapshot?.NetSales,
            d.Snapshot?.CountedCash,
            d.Snapshot?.CashVariance,
            d.Snapshot?.ShiftCount ?? d.Shifts.Count
        )).ToList();

        return new PagedResult<DaySummaryDto>(dtos, page, pageSize, total);
    }
}
