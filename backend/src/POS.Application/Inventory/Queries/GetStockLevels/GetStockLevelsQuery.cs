using MediatR;

namespace POS.Application.Inventory.Queries.GetStockLevels;

public record GetStockLevelsQuery : IRequest<IList<StockLevelDto>>;

public record StockLevelDto(
    Guid ItemId,
    string ItemName,
    string CategoryName,
    int Stock,
    int LowStockThreshold,
    bool IsLowStock,
    decimal CostPrice,
    decimal SellingPrice,
    decimal StockValue       
);