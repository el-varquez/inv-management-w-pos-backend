using MediatR;

namespace POS.Application.Inventory.Queries.GetInventoryCount;

public record InventoryCountLineDto(
    Guid ItemId,
    string ItemName,
    string CategoryName,
    int ExpectedQty,
    int? ActualQty);

public record InventoryCountDto(
    Guid Id,
    string Reference,
    string? Notes,
    string Status,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    IList<InventoryCountLineDto> Lines);

public record GetInventoryCountQuery(Guid Id) : IRequest<InventoryCountDto>;
