using MediatR;
using POS.Application.Common.Models;
using POS.Domain.Interfaces;

namespace POS.Application.Inventory.Queries.GetStockLevels;

public class GetStockLevelsQueryHandler
    : IRequestHandler<GetStockLevelsQuery, PagedResult<StockLevelDto>>
{
    private readonly IItemRepository _itemRepository;

    public GetStockLevelsQueryHandler(IItemRepository itemRepository)
        => _itemRepository = itemRepository;

    public async Task<PagedResult<StockLevelDto>> Handle(
        GetStockLevelsQuery request, CancellationToken ct)
    {
        var (page, pageSize) = Pagination.Normalize(request.Page, request.PageSize);
        var (items, total) = await _itemRepository.GetPagedAsync(page, pageSize, ct: ct);

        var dtos = items.Select(i => new StockLevelDto(
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

        return new PagedResult<StockLevelDto>(dtos, page, pageSize, total);
    }
}
