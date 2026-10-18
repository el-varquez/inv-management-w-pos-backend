using MediatR;
using POS.Application.Common.Models;
using POS.Domain.Interfaces;

namespace POS.Application.Inventory.Queries.GetInventoryValuation;

public class GetInventoryValuationQueryHandler
    : IRequestHandler<GetInventoryValuationQuery, InventoryValuationDto>
{
    private readonly IItemRepository _itemRepository;

    public GetInventoryValuationQueryHandler(IItemRepository itemRepository)
        => _itemRepository = itemRepository;

    public async Task<InventoryValuationDto> Handle(
        GetInventoryValuationQuery request, CancellationToken ct)
    {
        var (page, pageSize) = Pagination.Normalize(request.Page, request.PageSize);
        var (items, total, totalValue) = await _itemRepository.GetValuationPagedAsync(
            page, pageSize, request.Search, ct);

        var dtos = items
            .Select(i => new InventoryValuationItemDto(
                i.Id,
                i.Name,
                i.Category.Name,
                i.Stock,
                i.CostPrice,
                i.Stock * i.CostPrice))
            .ToList();

        return new InventoryValuationDto(
            dtos,
            page,
            pageSize,
            total,
            totalValue,
            total,
            DateTime.UtcNow
        );
    }
}
