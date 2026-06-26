using MediatR;
using POS.Domain.Enums;
using POS.Domain.Interfaces;

namespace POS.Application.Reports.Queries.GetExpenseReport;

public class GetExpenseReportQueryHandler
    : IRequestHandler<GetExpenseReportQuery, ExpenseReportDto>
{
    private readonly IStockMovementRepository _stockMovementRepository;

    public GetExpenseReportQueryHandler(IStockMovementRepository stockMovementRepository)
        => _stockMovementRepository = stockMovementRepository;

    public async Task<ExpenseReportDto> Handle(
        GetExpenseReportQuery request, CancellationToken ct)
    {
        var movements = await _stockMovementRepository.GetAllAsync(
            request.From, request.To, null, ct);

        var addStockMovements = movements
            .Where(m => m.Type == StockMovementType.AddStock && m.CostPerUnit.HasValue)
            .ToList();

        var costOfPurchases = addStockMovements
            .Sum(m => m.Quantity * (m.CostPerUnit ?? 0));

        var purchases = addStockMovements
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new ExpensePurchaseDto(
                m.CreatedAt,
                m.Item.Name,
                m.Quantity,
                m.CostPerUnit ?? 0,
                m.Quantity * (m.CostPerUnit ?? 0),
                m.SupplierName
            ))
            .ToList();

        var lossMovements = movements
            .Where(m =>
                (m.Type == StockMovementType.Adjustment ||
                 m.Type == StockMovementType.InventoryCount) &&
                m.Quantity < 0 &&
                (m.Reason == AdjustmentReason.Loss ||
                 m.Reason == AdjustmentReason.Damage ||
                 m.Reason == AdjustmentReason.Spoilage))
            .ToList();

        var inventoryLoss = lossMovements
            .Sum(m => Math.Abs(m.Quantity) * m.Item.CostPrice);

        var totalExpenses = costOfPurchases + inventoryLoss;

        var breakdown = new List<ExpenseLineDto>
        {
            new("Cost of Purchases", costOfPurchases),
            new("Inventory Loss", inventoryLoss)
        };

        return new ExpenseReportDto(
            costOfPurchases,
            inventoryLoss,
            totalExpenses,
            breakdown,
            purchases,
            request.From,
            request.To
        );
    }
}
