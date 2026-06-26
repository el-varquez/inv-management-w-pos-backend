using MediatR;

namespace POS.Application.Reports.Queries.GetExpenseReport;

public record GetExpenseReportQuery(
    DateTime? From,
    DateTime? To
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

public record ExpenseReportDto(
    decimal CostOfPurchases,
    decimal InventoryLoss,
    decimal TotalExpenses,
    IList<ExpenseLineDto> Breakdown,
    IList<ExpensePurchaseDto> Purchases,
    DateTime? From,
    DateTime? To
);
