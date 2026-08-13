using MediatR;
using POS.Application.Common;
using POS.Domain.Interfaces;

namespace POS.Application.Items.Queries.SearchItems;

public class SearchItemsQueryHandler
    : IRequestHandler<SearchItemsQuery, IList<SearchItemDto>>
{
    private const int DefaultLimit = 10;
    private const int MaxLimit = 25;

    private readonly IItemRepository _itemRepository;
    private readonly ICompositeItemRepository _compositeItemRepository;

    public SearchItemsQueryHandler(
        IItemRepository itemRepository,
        ICompositeItemRepository compositeItemRepository)
    {
        _itemRepository = itemRepository;
        _compositeItemRepository = compositeItemRepository;
    }

    public async Task<IList<SearchItemDto>> Handle(SearchItemsQuery request, CancellationToken ct)
    {
        var limit = Math.Clamp(request.Limit ?? DefaultLimit, 1, MaxLimit);
        var items = await _itemRepository.SearchAsync(request.Term.Trim(), limit, ct);

        var dtos = new List<SearchItemDto>();
        foreach (var i in items)
        {
            var stock = i.IsComposite
                ? CompositeStock.Buildable(
                    await _compositeItemRepository.GetByParentIdAsync(i.Id, ct))
                : i.Stock;

            dtos.Add(new SearchItemDto(
                i.Id,
                i.Name,
                i.Barcode,
                i.Sku,
                stock,
                i.CostPrice,
                i.SellingPrice,
                i.IsActive,
                i.IsComposite,
                i.Category.Name
            ));
        }

        return dtos;
    }
}
