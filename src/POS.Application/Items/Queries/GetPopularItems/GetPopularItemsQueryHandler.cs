using MediatR;
using POS.Application.Common;
using POS.Domain.Interfaces;

namespace POS.Application.Items.Queries.GetPopularItems;

public class GetPopularItemsQueryHandler
    : IRequestHandler<GetPopularItemsQuery, IList<PopularItemDto>>
{
    private const int TileCount = 8;
    private const int WindowDays = 7;

    private readonly IItemRepository _itemRepository;
    private readonly ICompositeItemRepository _compositeItemRepository;
    private readonly ITransactionRepository _transactionRepository;

    public GetPopularItemsQueryHandler(
        IItemRepository itemRepository,
        ICompositeItemRepository compositeItemRepository,
        ITransactionRepository transactionRepository)
    {
        _itemRepository = itemRepository;
        _compositeItemRepository = compositeItemRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task<IList<PopularItemDto>> Handle(GetPopularItemsQuery request, CancellationToken ct)
    {
        var transactions = await _transactionRepository.GetAllAsync(
            DateTime.UtcNow.AddDays(-WindowDays), null, ct);

        var ranked = transactions
            .SelectMany(t => t.Items)
            .GroupBy(i => i.ItemId)
            .Select(g => (ItemId: g.Key, Sold: g.Sum(i => i.Quantity)))
            .Where(x => x.Sold > 0)
            .OrderByDescending(x => x.Sold)
            .ToList();

        var dtos = new List<PopularItemDto>();
        foreach (var (itemId, sold) in ranked)
        {
            if (dtos.Count == TileCount)
            {
                break;
            }
            var item = await _itemRepository.GetByIdAsync(itemId, ct);
            if (item is null || !item.IsActive)
            {
                continue;
            }
            var stock = item.IsComposite
                ? CompositeStock.Buildable(await _compositeItemRepository.GetByParentIdAsync(item.Id, ct))
                : item.Stock;
            dtos.Add(new PopularItemDto(
                item.Id, item.Name, item.Barcode, item.ItemCode, item.SellingPrice,
                item.UtangMarkup, stock, item.IsComposite, item.TracksStock, sold));
        }
        return dtos;
    }
}
