using MediatR;
using POS.Application.Common;
using POS.Domain.Interfaces;

namespace POS.Application.Items.Queries.GetPopularItems;

public class GetPopularItemsQueryHandler
    : IRequestHandler<GetPopularItemsQuery, IList<PopularItemDto>>
{
    private const int TileCount = 8;
    private const int WindowDays = 7;

    private readonly IItemRepository _items;
    private readonly ICompositeItemRepository _composites;
    private readonly ISaleRepository _sales;
    private readonly IInvoiceRepository _invoices;

    public GetPopularItemsQueryHandler(
        IItemRepository items,
        ICompositeItemRepository composites,
        ISaleRepository sales,
        IInvoiceRepository invoices)
    {
        _items = items;
        _composites = composites;
        _sales = sales;
        _invoices = invoices;
    }

    public async Task<IList<PopularItemDto>> Handle(GetPopularItemsQuery request, CancellationToken ct)
    {
        var since = DateTime.UtcNow.AddDays(-WindowDays);
        var sales = await _sales.GetAllAsync(since, null, ct);
        var invoices = await _invoices.GetAllAsync(since, null, ct);

        var ranked = sales
            .SelectMany(s => s.Items.Select(i => (i.ItemId, i.Quantity)))
            .Concat(invoices
                .Where(i => !i.IsVoided)
                .SelectMany(i => i.Items.Select(l => (l.ItemId, l.Quantity))))
            .GroupBy(x => x.ItemId)
            .Select(g => (ItemId: g.Key, Sold: g.Sum(x => x.Quantity)))
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
            var item = await _items.GetByIdAsync(itemId, ct);
            if (item is null || !item.IsActive)
            {
                continue;
            }
            var stock = item.IsComposite
                ? CompositeStock.Buildable(await _composites.GetByParentIdAsync(item.Id, ct))
                : item.Stock;
            dtos.Add(new PopularItemDto(
                item.Id, item.Name, item.Barcode, item.ItemCode, item.SellingPrice,
                item.UtangMarkup, stock, item.IsComposite, item.TracksStock, sold));
        }
        return dtos;
    }
}
