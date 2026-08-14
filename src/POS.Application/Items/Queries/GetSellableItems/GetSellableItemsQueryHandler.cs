using MediatR;
using POS.Application.Common;
using POS.Application.Items.Queries.GetItems;
using POS.Domain.Entities;
using POS.Domain.Interfaces;

namespace POS.Application.Items.Queries.GetSellableItems;

public class GetSellableItemsQueryHandler : IRequestHandler<GetSellableItemsQuery, IList<ItemDto>>
{
    private readonly IItemRepository _itemRepository;
    private readonly ICompositeItemRepository _compositeItemRepository;

    public GetSellableItemsQueryHandler(
        IItemRepository itemRepository,
        ICompositeItemRepository compositeItemRepository)
    {
        _itemRepository = itemRepository;
        _compositeItemRepository = compositeItemRepository;
    }

    public async Task<IList<ItemDto>> Handle(GetSellableItemsQuery request, CancellationToken ct)
    {
        var items = await _itemRepository.GetAllAsync(ct);

        var dtos = new List<ItemDto>();
        foreach (var i in items)
        {
            var stock = await StockOfAsync(i, ct);
            dtos.Add(new ItemDto(
                i.Id,
                i.Name,
                i.Description,
                i.ItemCode,
                i.Barcode,
                i.CostPrice,
                i.SellingPrice,
                i.UtangMarkup,
                stock,
                i.LowStockThreshold,
                stock <= i.LowStockThreshold,
                i.IsActive,
                i.IsComposite,
                i.CategoryId,
                i.Category.Name,
                i.CreatedAt
            ));
        }

        return dtos;
    }

    private async Task<int> StockOfAsync(Item item, CancellationToken ct)
        => item.IsComposite
            ? CompositeStock.Buildable(await _compositeItemRepository.GetByParentIdAsync(item.Id, ct))
            : item.Stock;
}
