using MediatR;
using POS.Domain.Interfaces;

namespace POS.Application.Inventory.Queries.GetInventoryValuation;

public class GetInventoryValuationQueryHandler
    : IRequestHandler<GetInventoryValuationQuery, InventoryValuationDto>
{
    private readonly IItemRepository _itemRepository;

    public GetInventoryValuationQueryHandler(IItemRepository itemRepository)
        => _itemRepository = itemRepository;

    public async Task<InventoryValuationDto> Handle(
        GetInventoryValuationQuery request, CancellationToken ct)
    {
        var items = await _itemRepository.GetAllAsync(ct);

        var valuationItems = items.Select(i => new InventoryValuationItemDto(
            i.Id,
            i.Name,
            i.Category.Name,
            i.Stock,
            i.CostPrice,
            i.Stock * i.CostPrice
        )).ToList();

        return new InventoryValuationDto(
            valuationItems,
            valuationItems.Sum(i => i.StockValue),
            valuationItems.Count,
            DateTime.UtcNow
        );
    }
}