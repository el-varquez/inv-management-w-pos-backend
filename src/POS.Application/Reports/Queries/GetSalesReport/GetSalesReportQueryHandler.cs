using MediatR;
using POS.Application.Common.Models;
using POS.Domain.Interfaces;

namespace POS.Application.Reports.Queries.GetSalesReport;

public class GetSalesReportQueryHandler
    : IRequestHandler<GetSalesReportQuery, SalesReportDto>
{
    private readonly ISaleRepository _sales;

    public GetSalesReportQueryHandler(ISaleRepository saleRepository)
        => _sales = saleRepository;

    public async Task<SalesReportDto> Handle(
        GetSalesReportQuery request, CancellationToken ct)
    {
        var (page, pageSize) = Pagination.Normalize(request.Page, request.PageSize);

        var totals = await _sales.GetTotalsAsync(request.From, request.To, ct);
        var (rows, total) = await _sales.GetDailyRowsPagedAsync(
            request.From, request.To, page, pageSize, ct);

        var items = rows
            .Select(r => new SalesReportDailyDto(
                r.Date, r.Gross, r.Discounts, r.Refunds, r.Net, r.Count))
            .ToList();

        return new SalesReportDto(
            items,
            page,
            pageSize,
            total,
            totals.Gross,
            totals.Discounts,
            totals.Refunds,
            totals.Net,
            totals.Count,
            request.From,
            request.To
        );
    }
}
