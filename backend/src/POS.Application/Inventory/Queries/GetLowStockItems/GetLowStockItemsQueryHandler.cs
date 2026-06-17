using MediatR;
using POS.Domain.Interfaces;

namespace POS.Application.Inventory.Queries.GetLowStockItems;

public class GetLowStockItemsQueryHandler
    : IRequestHandler<GetLowStockItemsQuery, IList<LowStockItemDto>>
{
    private readonly IItemRepository _itemRepository;

    public GetLowStockItemsQueryHandler(IItemRepository itemRepository)
        => _itemRepository = itemRepository;

    public async Task<IList<LowStockItemDto>> Handle(
        GetLowStockItemsQuery request, CancellationToken ct)
    {
        var items = await _itemRepository.GetLowStockAsync(ct);

        return items.Select(i => new LowStockItemDto(
            i.Id,
            i.Name,
            i.Category.Name,
            i.Stock,
            i.LowStockThreshold,
            i.LowStockThreshold - i.Stock
        )).ToList();
    }
}