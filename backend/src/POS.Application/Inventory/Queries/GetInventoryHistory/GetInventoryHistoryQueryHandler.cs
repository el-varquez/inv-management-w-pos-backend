using MediatR;
using POS.Domain.Interfaces;

namespace POS.Application.Inventory.Queries.GetInventoryHistory;

public class GetInventoryHistoryQueryHandler
    : IRequestHandler<GetInventoryHistoryQuery, IList<InventoryHistoryDto>>
{
    private readonly IStockMovementRepository _stockMovementRepository;

    public GetInventoryHistoryQueryHandler(IStockMovementRepository stockMovementRepository)
        => _stockMovementRepository = stockMovementRepository;

    public async Task<IList<InventoryHistoryDto>> Handle(
        GetInventoryHistoryQuery request, CancellationToken ct)
    {
        var movements = await _stockMovementRepository.GetAllAsync(
            request.From, request.To, request.Type, ct);

        return movements.Select(m => new InventoryHistoryDto(
            m.Id,
            m.ItemId,
            m.Item.Name,
            m.Item.Category.Name,
            m.Type.ToString(),
            m.Quantity,
            m.CostPerUnit,
            m.CostPerUnit.HasValue ? m.Quantity * m.CostPerUnit.Value : null,
            m.SupplierName,
            m.Reason?.ToString(),
            m.Notes,
            m.CreatedAt
        )).ToList();
    }
}