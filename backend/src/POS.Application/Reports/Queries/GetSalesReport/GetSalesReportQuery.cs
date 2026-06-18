using MediatR;

namespace POS.Application.Reports.Queries.GetSalesReport;

public record GetSalesReportQuery(
    DateTime? From,
    DateTime? To
) : IRequest<SalesReportDto>;

public record SalesReportDailyDto(
    DateTime Date,
    decimal GrossSales,
    decimal Discounts,
    decimal Refunds,
    decimal NetSales,
    int TransactionCount
);

public record SalesReportDto(
    decimal GrossSales,
    decimal TotalDiscounts,
    decimal TotalRefunds,
    decimal NetSales,
    int TransactionCount,
    IList<SalesReportDailyDto> DailyBreakdown,
    DateTime? From,
    DateTime? To
);
