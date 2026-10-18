using MediatR;
using POS.Application.Common.Models;

namespace POS.Application.Inventory.Queries.GetLowStockItems;

public record GetLowStockItemsQuery(int? Page, int? PageSize) : IRequest<PagedResult<LowStockItemDto>>;

public record LowStockItemDto(
    Guid ItemId,
    string ItemName,
    string CategoryName,
    int Stock,
    int LowStockThreshold,
    int Deficit            
);
