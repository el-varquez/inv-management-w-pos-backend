using MediatR;
using POS.Domain.Interfaces;

namespace POS.Application.Inventory.Queries.GetStockLevels;

public class GetStockLevelsQueryHandler
    : IRequestHandler<GetStockLevelsQuery, IList<StockLevelDto>>
{
    private readonly IItemRepository _itemRepository;

    public GetStockLevelsQueryHandler(IItemRepository itemRepository)
        => _itemRepository = itemRepository;

    public async Task<IList<StockLevelDto>> Handle(
        GetStockLevelsQuery request, CancellationToken ct)
    {
        var items = await _itemRepository.GetAllAsync(ct);

        return items.Select(i => new StockLevelDto(
            i.Id,
            i.Name,
            i.Category.Name,
            i.Stock,
            i.LowStockThreshold,
            i.Stock <= i.LowStockThreshold,
            i.CostPrice,
            i.SellingPrice,
            i.Stock * i.CostPrice
        )).ToList();
    }
}