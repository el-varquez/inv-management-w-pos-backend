using MediatR;
using POS.Application.Common.Models;

namespace POS.Application.Reports.Queries.GetSalesReport;

public record GetSalesReportQuery(
    DateTime? From,
    DateTime? To,
    int? Page,
    int? PageSize
) : IRequest<SalesReportDto>;

public record SalesReportDailyDto(
    DateTime Date,
    decimal GrossSales,
    decimal Discounts,
    decimal Refunds,
    decimal NetSales,
    int TransactionCount
);

/// <summary>One page of daily rows plus the totals over the whole range.</summary>
public record SalesReportDto(
    IReadOnlyList<SalesReportDailyDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    decimal GrossSales,
    decimal TotalDiscounts,
    decimal TotalRefunds,
    decimal NetSales,
    int TransactionCount,
    DateTime? From,
    DateTime? To
) : PagedResult<SalesReportDailyDto>(Items, Page, PageSize, TotalCount);
