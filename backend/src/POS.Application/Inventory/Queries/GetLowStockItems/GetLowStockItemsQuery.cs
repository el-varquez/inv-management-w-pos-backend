using MediatR;

namespace POS.Application.Inventory.Queries.GetLowStockItems;

public record GetLowStockItemsQuery : IRequest<IList<LowStockItemDto>>;

public record LowStockItemDto(
    Guid ItemId,
    string ItemName,
    string CategoryName,
    int Stock,
    int LowStockThreshold,
    int Deficit            
);