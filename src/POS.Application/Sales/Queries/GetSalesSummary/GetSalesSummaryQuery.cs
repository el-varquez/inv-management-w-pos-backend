using MediatR;

namespace POS.Application.Sales.Queries.GetSalesSummary;

public record GetSalesSummaryQuery(
    DateTime? From,
    DateTime? To
) : IRequest<SalesSummaryDto>;

public record SalesSummaryDto(
    decimal GrossSales,
    decimal TotalDiscounts,
    decimal Refunds,
    decimal NetSales,
    int TransactionCount,
    DateTime? From,
    DateTime? To
);