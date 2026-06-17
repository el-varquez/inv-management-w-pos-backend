using MediatR;
using POS.Domain.Enums;

namespace POS.Application.Inventory.Queries.GetInventoryHistory;

public record GetInventoryHistoryQuery(
    DateTime? From,
    DateTime? To,
    StockMovementType? Type
) : IRequest<IList<InventoryHistoryDto>>;

public record InventoryHistoryDto(
    Guid Id,
    Guid ItemId,
    string ItemName,
    string CategoryName,
    string MovementType,
    int Quantity,
    decimal? CostPerUnit,
    decimal? TotalCost,
    string? SupplierName,
    string? Reason,
    string? Notes,
    DateTime CreatedAt
);