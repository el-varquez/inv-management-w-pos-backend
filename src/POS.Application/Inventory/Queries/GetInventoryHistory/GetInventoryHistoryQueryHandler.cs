using MediatR;
using POS.Application.Common.Models;
using POS.Domain.Interfaces;

namespace POS.Application.Inventory.Queries.GetInventoryHistory;

public class GetInventoryHistoryQueryHandler
    : IRequestHandler<GetInventoryHistoryQuery, PagedResult<InventoryHistoryDto>>
{
    private readonly IStockMovementRepository _stockMovementRepository;

    public GetInventoryHistoryQueryHandler(IStockMovementRepository stockMovementRepository)
        => _stockMovementRepository = stockMovementRepository;

    public async Task<PagedResult<InventoryHistoryDto>> Handle(
        GetInventoryHistoryQuery request, CancellationToken ct)
    {
        var (page, pageSize) = Pagination.Normalize(request.Page, request.PageSize);
        var (movements, total) = await _stockMovementRepository.GetPagedAsync(
            request.From, request.To, request.Type, page, pageSize, ct);

        var dtos = movements.Select(m => new InventoryHistoryDto(
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

        return new PagedResult<InventoryHistoryDto>(dtos, page, pageSize, total);
    }
}
