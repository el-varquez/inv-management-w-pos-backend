using MediatR;

namespace POS.Application.Inventory.Queries.GetInventoryCount;

public record InventoryCountDto(
    Guid Id,
    string Reference,
    string? Notes,
    string Status,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    int LineCount,
    int CountedCount,
    int VarianceCount);

public record GetInventoryCountQuery(Guid Id) : IRequest<InventoryCountDto>;
