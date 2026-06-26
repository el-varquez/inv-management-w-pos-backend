using MediatR;
using POS.Application.Common.Models;

namespace POS.Application.Inventory.Queries.GetStockLevels;

public record GetStockLevelsQuery(int? Page, int? PageSize) : IRequest<PagedResult<StockLevelDto>>;

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