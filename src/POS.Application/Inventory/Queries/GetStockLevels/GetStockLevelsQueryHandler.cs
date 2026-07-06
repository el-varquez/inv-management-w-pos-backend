using MediatR;
using POS.Application.Common;
using POS.Application.Common.Models;
using POS.Domain.Entities;
using POS.Domain.Interfaces;

namespace POS.Application.Inventory.Queries.GetStockLevels;

public class GetStockLevelsQueryHandler
    : IRequestHandler<GetStockLevelsQuery, PagedResult<StockLevelDto>>
{
    private readonly IItemRepository _itemRepository;
    private readonly ICompositeItemRepository _compositeItemRepository;

    public GetStockLevelsQueryHandler(
        IItemRepository itemRepository,
        ICompositeItemRepository compositeItemRepository)
    {
        _itemRepository = itemRepository;
        _compositeItemRepository = compositeItemRepository;
    }

    public async Task<PagedResult<StockLevelDto>> Handle(
        GetStockLevelsQuery request, CancellationToken ct)
    {
        var (page, pageSize) = Pagination.Normalize(request.Page, request.PageSize);
        var (items, total) = await _itemRepository.GetPagedAsync(page, pageSize, ct: ct);

        var dtos = new List<StockLevelDto>();
        foreach (var i in items)
        {
            var stock = await StockOfAsync(i, ct);
            dtos.Add(new StockLevelDto(
                i.Id,
                i.Name,
                i.Category.Name,
                stock,
                i.LowStockThreshold,
                stock <= i.LowStockThreshold,
                i.CostPrice,
                i.SellingPrice,
                i.IsComposite ? 0m : stock * i.CostPrice
            ));
        }

        return new PagedResult<StockLevelDto>(dtos, page, pageSize, total);
    }

    private async Task<int> StockOfAsync(Item item, CancellationToken ct)
        => item.IsComposite
            ? CompositeStock.Buildable(await _compositeItemRepository.GetByParentIdAsync(item.Id, ct))
            : item.Stock;
}
