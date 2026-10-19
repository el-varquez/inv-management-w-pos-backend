using MediatR;
using POS.Application.Common.Models;

namespace POS.Application.Reports.Queries.GetProfitReport;

public record GetProfitReportQuery(
    DateTime? From,
    DateTime? To,
    Guid? CategoryId,
    Guid? ItemId,
    int? Page,
    int? PageSize
) : IRequest<ProfitReportDto>;

public record ProfitDetailDto(
    Guid ItemId,
    string ItemName,
    string CategoryName,
    int QuantitySold,
    decimal Revenue,
    decimal Cost,
    decimal Profit,
    decimal MarginPercent
);

/// <summary>One page of per-item rows (most profitable first) plus the totals over the whole range and filter.</summary>
public record ProfitReportDto(
    IReadOnlyList<ProfitDetailDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    decimal NetSales,
    decimal CostOfGoodsSold,
    decimal GrossProfit,
    decimal InventoryLoss,
    decimal NetProfit,
    decimal GrossMarginPercent,
    DateTime? From,
    DateTime? To
) : PagedResult<ProfitDetailDto>(Items, Page, PageSize, TotalCount);
