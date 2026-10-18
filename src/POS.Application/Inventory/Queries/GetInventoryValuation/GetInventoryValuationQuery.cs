using MediatR;
using POS.Application.Common.Models;

namespace POS.Application.Inventory.Queries.GetInventoryValuation;

public record GetInventoryValuationQuery(int? Page, int? PageSize) : IRequest<InventoryValuationDto>;

public record InventoryValuationItemDto(
    Guid ItemId,
    string ItemName,
    string CategoryName,
    int Stock,
    decimal CostPrice,
    decimal StockValue
);

public record InventoryValuationDto(
    IReadOnlyList<InventoryValuationItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    decimal TotalValue,
    int TotalItems,
    DateTime GeneratedAt
) : PagedResult<InventoryValuationItemDto>(Items, Page, PageSize, TotalCount);
