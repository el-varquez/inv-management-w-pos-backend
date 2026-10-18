using MediatR;
using POS.Application.Common.Models;
using POS.Domain.Interfaces;

namespace POS.Application.Inventory.Queries.GetLowStockItems;

public class GetLowStockItemsQueryHandler
    : IRequestHandler<GetLowStockItemsQuery, PagedResult<LowStockItemDto>>
{
    private readonly IItemRepository _itemRepository;

    public GetLowStockItemsQueryHandler(IItemRepository itemRepository)
        => _itemRepository = itemRepository;

    public async Task<PagedResult<LowStockItemDto>> Handle(
        GetLowStockItemsQuery request, CancellationToken ct)
    {
        var (page, pageSize) = Pagination.Normalize(request.Page, request.PageSize);
        var (items, total) = await _itemRepository.GetLowStockPagedAsync(page, pageSize, ct);

        var dtos = items
            .Select(i => new LowStockItemDto(
                i.Id,
                i.Name,
                i.Category.Name,
                i.Stock,
                i.LowStockThreshold,
                i.LowStockThreshold - i.Stock))
            .ToList();

        return new PagedResult<LowStockItemDto>(dtos, page, pageSize, total);
    }
}
