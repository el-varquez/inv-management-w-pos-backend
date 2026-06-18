using MediatR;
using POS.Application.Common.Models;
using POS.Domain.Interfaces;

namespace POS.Application.Items.Queries.GetItems;

public class GetItemsQueryHandler : IRequestHandler<GetItemsQuery, PagedResult<ItemDto>>
{
    private readonly IItemRepository _itemRepository;

    public GetItemsQueryHandler(IItemRepository itemRepository)
        => _itemRepository = itemRepository;

    public async Task<PagedResult<ItemDto>> Handle(GetItemsQuery request, CancellationToken ct)
    {
        var (page, pageSize) = Pagination.Normalize(request.Page, request.PageSize);
        var (items, total) = await _itemRepository.GetPagedAsync(page, pageSize, ct);

        var dtos = items.Select(i => new ItemDto(
            i.Id,
            i.Name,
            i.Description,
            i.Sku,
            i.CostPrice,
            i.SellingPrice,
            i.Stock,
            i.LowStockThreshold,
            i.Stock <= i.LowStockThreshold,
            i.IsActive,
            i.CategoryId,
            i.Category.Name,
            i.CreatedAt
        )).ToList();

        return new PagedResult<ItemDto>(dtos, page, pageSize, total);
    }
}
