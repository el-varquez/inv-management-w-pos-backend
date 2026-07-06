using MediatR;
using POS.Application.Common.Models;
using POS.Domain.Interfaces;

namespace POS.Application.Inventory.Queries.GetInventoryCounts;

public class GetInventoryCountsQueryHandler
    : IRequestHandler<GetInventoryCountsQuery, PagedResult<InventoryCountSummaryDto>>
{
    private readonly IInventoryCountRepository _countRepository;

    public GetInventoryCountsQueryHandler(IInventoryCountRepository countRepository)
        => _countRepository = countRepository;

    public async Task<PagedResult<InventoryCountSummaryDto>> Handle(
        GetInventoryCountsQuery request, CancellationToken ct)
    {
        var (page, pageSize) = Pagination.Normalize(request.Page, request.PageSize);
        var (counts, total) = await _countRepository.GetPagedAsync(
            request.Status, page, pageSize, ct);

        var dtos = counts.Select(c => new InventoryCountSummaryDto(
            c.Id,
            c.Reference,
            c.Status.ToString(),
            c.CreatedAt,
            c.CompletedAt,
            c.Lines.Count)).ToList();

        return new PagedResult<InventoryCountSummaryDto>(dtos, page, pageSize, total);
    }
}
