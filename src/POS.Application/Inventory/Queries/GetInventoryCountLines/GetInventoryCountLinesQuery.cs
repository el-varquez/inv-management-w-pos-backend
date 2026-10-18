using MediatR;
using POS.Application.Common.Models;

namespace POS.Application.Inventory.Queries.GetInventoryCountLines;

public record InventoryCountLineDto(
    Guid ItemId,
    string ItemName,
    string CategoryName,
    int ExpectedQty,
    int? ActualQty);

public record GetInventoryCountLinesQuery(
    Guid CountId,
    int? Page,
    int? PageSize,
    string? Search = null
) : IRequest<PagedResult<InventoryCountLineDto>>;
