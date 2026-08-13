using MediatR;
using POS.Application.Common;
using POS.Application.Common.Models;
using POS.Domain.Entities;
using POS.Domain.Interfaces;

namespace POS.Application.Items.Queries.GetItems;

public class GetItemsQueryHandler : IRequestHandler<GetItemsQuery, PagedResult<ItemDto>>
{
    private readonly IItemRepository _itemRepository;
    private readonly ICompositeItemRepository _compositeItemRepository;

    public GetItemsQueryHandler(
        IItemRepository itemRepository,
        ICompositeItemRepository compositeItemRepository)
    {
        _itemRepository = itemRepository;
        _compositeItemRepository = compositeItemRepository;
    }

    public async Task<PagedResult<ItemDto>> Handle(GetItemsQuery request, CancellationToken ct)
    {
        var (page, pageSize) = Pagination.Normalize(request.Page, request.PageSize);
        var (items, total) = await _itemRepository.GetPagedAsync(
            page, pageSize, request.IsComposite, ct);

        var dtos = new List<ItemDto>();
        foreach (var i in items)
        {
            var stock = await StockOfAsync(i, ct);
            dtos.Add(new ItemDto(
                i.Id,
                i.Name,
                i.Description,
                i.Sku,
                i.Barcode,
                i.CostPrice,
                i.SellingPrice,
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

        return new PagedResult<ItemDto>(dtos, page, pageSize, total);
    }

    private async Task<int> StockOfAsync(Item item, CancellationToken ct)
        => item.IsComposite
            ? CompositeStock.Buildable(await _compositeItemRepository.GetByParentIdAsync(item.Id, ct))
            : item.Stock;
}
