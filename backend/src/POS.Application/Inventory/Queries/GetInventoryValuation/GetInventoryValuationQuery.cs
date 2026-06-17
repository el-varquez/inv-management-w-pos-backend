using MediatR;

namespace POS.Application.Inventory.Queries.GetInventoryValuation;

public record GetInventoryValuationQuery : IRequest<InventoryValuationDto>;

public record InventoryValuationItemDto(
    Guid ItemId,
    string ItemName,
    string CategoryName,
    int Stock,
    decimal CostPrice,
    decimal StockValue
);

public record InventoryValuationDto(
    IList<InventoryValuationItemDto> Items,
    decimal TotalValue,
    int TotalItems,
    DateTime GeneratedAt
);