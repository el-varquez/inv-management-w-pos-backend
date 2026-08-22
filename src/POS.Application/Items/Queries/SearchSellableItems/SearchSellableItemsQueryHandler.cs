using MediatR;
using POS.Application.Common;
using POS.Domain.Interfaces;

namespace POS.Application.Items.Queries.SearchSellableItems;

public class SearchSellableItemsQueryHandler
    : IRequestHandler<SearchSellableItemsQuery, IList<SellableItemDto>>
{
    private const int DefaultLimit = 8;
    private const int MaxLimit = 25;

    private readonly IItemRepository _itemRepository;
    private readonly ICompositeItemRepository _compositeItemRepository;

    public SearchSellableItemsQueryHandler(
        IItemRepository itemRepository,
        ICompositeItemRepository compositeItemRepository)
    {
        _itemRepository = itemRepository;
        _compositeItemRepository = compositeItemRepository;
    }

    public async Task<IList<SellableItemDto>> Handle(
        SearchSellableItemsQuery request, CancellationToken ct)
    {
        var term = request.Term.Trim();
        if (term.Length == 0)
        {
            return [];
        }
        var limit = Math.Clamp(request.Limit ?? DefaultLimit, 1, MaxLimit);
        var items = await _itemRepository.SearchActiveAsync(term, limit, ct);

        var dtos = new List<SellableItemDto>();
        foreach (var i in items)
        {
            var stock = i.IsComposite
                ? CompositeStock.Buildable(await _compositeItemRepository.GetByParentIdAsync(i.Id, ct))
                : i.Stock;
            dtos.Add(new SellableItemDto(
                i.Id, i.Name, i.Barcode, i.ItemCode, i.SellingPrice, i.UtangMarkup, stock, i.IsComposite, i.TracksStock));
        }
        return dtos;
    }
}
