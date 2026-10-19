using MediatR;
using POS.Application.Common.Models;

namespace POS.Application.Reports.Queries.GetExpenseReport;

public record GetExpenseReportQuery(
    DateTime? From,
    DateTime? To,
    int? Page,
    int? PageSize
) : IRequest<ExpenseReportDto>;

public record ExpenseLineDto(
    string Category,
    decimal Amount
);

public record ExpensePurchaseDto(
    DateTime Date,
    string ItemName,
    int Quantity,
    decimal CostPerUnit,
    decimal TotalCost,
    string? SupplierName
);

/// <summary>One page of purchases (newest first) plus the totals over the whole range.</summary>
public record ExpenseReportDto(
    IReadOnlyList<ExpensePurchaseDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    decimal CostOfPurchases,
    decimal InventoryLoss,
    decimal TotalExpenses,
    IList<ExpenseLineDto> Breakdown,
    DateTime? From,
    DateTime? To
) : PagedResult<ExpensePurchaseDto>(Items, Page, PageSize, TotalCount);
