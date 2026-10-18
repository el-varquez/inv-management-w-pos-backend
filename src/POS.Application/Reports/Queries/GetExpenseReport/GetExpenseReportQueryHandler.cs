using MediatR;
using POS.Application.Common.Models;
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
        var (page, pageSize) = Pagination.Normalize(request.Page, request.PageSize);

        var costOfPurchases = await _stockMovementRepository.GetPurchaseTotalAsync(
            request.From, request.To, ct);
        var inventoryLoss = await _stockMovementRepository.GetLossTotalAsync(
            request.From, request.To, null, null, ct);
        var (purchases, total) = await _stockMovementRepository.GetPurchasesPagedAsync(
            request.From, request.To, page, pageSize, ct);

        var items = purchases
            .Select(m => new ExpensePurchaseDto(
                m.CreatedAt,
                m.Item.Name,
                m.Quantity,
                m.CostPerUnit ?? 0,
                m.Quantity * (m.CostPerUnit ?? 0),
                m.SupplierName
            ))
            .ToList();

        var breakdown = new List<ExpenseLineDto>
        {
            new("Cost of Purchases", costOfPurchases),
            new("Inventory Loss", inventoryLoss)
        };

        return new ExpenseReportDto(
            items,
            page,
            pageSize,
            total,
            costOfPurchases,
            inventoryLoss,
            costOfPurchases + inventoryLoss,
            breakdown,
            request.From,
            request.To
        );
    }
}
