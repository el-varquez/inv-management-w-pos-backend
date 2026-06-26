using MediatR;
using POS.Application.Common.Models;

namespace POS.Application.Items.Queries.GetItems;

public record GetItemsQuery(int? Page, int? PageSize, bool? IsComposite = null)
    : IRequest<PagedResult<ItemDto>>;

public record ItemDto(
    Guid Id,
    string Name,
    string? Description,
    string? Sku,
    decimal CostPrice,
    decimal SellingPrice,
    int Stock,
    int LowStockThreshold,
    bool IsLowStock,
    bool IsActive,
    bool IsComposite,
    Guid CategoryId,
    string CategoryName,
    DateTime CreatedAt
);