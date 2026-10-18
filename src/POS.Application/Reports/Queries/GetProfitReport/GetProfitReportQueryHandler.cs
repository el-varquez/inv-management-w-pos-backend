using MediatR;
using POS.Application.Common.Models;
using POS.Domain.Interfaces;

namespace POS.Application.Reports.Queries.GetProfitReport;

public class GetProfitReportQueryHandler
    : IRequestHandler<GetProfitReportQuery, ProfitReportDto>
{
    private readonly ISaleRepository _sales;
    private readonly IStockMovementRepository _stockMovementRepository;

    public GetProfitReportQueryHandler(
        ISaleRepository saleRepository,
        IStockMovementRepository stockMovementRepository)
    {
        _sales = saleRepository;
        _stockMovementRepository = stockMovementRepository;
    }

    public async Task<ProfitReportDto> Handle(
        GetProfitReportQuery request, CancellationToken ct)
    {
        var (page, pageSize) = Pagination.Normalize(request.Page, request.PageSize);
        var isFiltered = request.ItemId.HasValue || request.CategoryId.HasValue;

        // Unfiltered net sales is the sales table's paid total minus refunds; filtered net sales
        // is the matching lines' revenue. Cost always comes from the matching lines.
        var lineTotals = await _sales.GetLineTotalsAsync(
            request.From, request.To, request.CategoryId, request.ItemId, ct);
        var netSales = isFiltered
            ? lineTotals.Revenue
            : (await _sales.GetTotalsAsync(request.From, request.To, ct)).Net;
        var cogs = lineTotals.Cost;
        var grossProfit = netSales - cogs;

        var inventoryLoss = await _stockMovementRepository.GetLossTotalAsync(
            request.From, request.To, request.CategoryId, request.ItemId, ct);
        var netProfit = grossProfit - inventoryLoss;

        var grossMargin = netSales != 0
            ? Math.Round(grossProfit / netSales * 100, 2)
            : 0;

        var (rows, total) = await _sales.GetItemProfitRowsPagedAsync(
            request.From, request.To, request.CategoryId, request.ItemId, page, pageSize, ct);

        var items = rows
            .Select(r =>
            {
                var profit = r.Revenue - r.Cost;
                return new ProfitDetailDto(
                    r.ItemId,
                    r.ItemName,
                    r.CategoryName,
                    r.Quantity,
                    r.Revenue,
                    r.Cost,
                    profit,
                    r.Revenue != 0 ? Math.Round(profit / r.Revenue * 100, 2) : 0
                );
            })
            .ToList();

        return new ProfitReportDto(
            items,
            page,
            pageSize,
            total,
            netSales,
            cogs,
            grossProfit,
            inventoryLoss,
            netProfit,
            grossMargin,
            request.From,
            request.To
        );
    }
}
