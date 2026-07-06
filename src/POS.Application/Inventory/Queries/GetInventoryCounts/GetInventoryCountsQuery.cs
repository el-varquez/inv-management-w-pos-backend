using MediatR;
using POS.Application.Common.Models;
using POS.Domain.Enums;

namespace POS.Application.Inventory.Queries.GetInventoryCounts;

public record InventoryCountSummaryDto(
    Guid Id,
    string Reference,
    string Status,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    int LineCount);

public record GetInventoryCountsQuery(
    InventoryCountStatus? Status,
    int? Page,
    int? PageSize
) : IRequest<PagedResult<InventoryCountSummaryDto>>;
