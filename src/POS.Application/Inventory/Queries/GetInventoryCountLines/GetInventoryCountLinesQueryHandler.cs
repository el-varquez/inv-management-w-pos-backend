using MediatR;
using POS.Application.Common.Models;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Inventory.Queries.GetInventoryCountLines;

public class GetInventoryCountLinesQueryHandler
    : IRequestHandler<GetInventoryCountLinesQuery, PagedResult<InventoryCountLineDto>>
{
    private readonly IInventoryCountRepository _countRepository;

    public GetInventoryCountLinesQueryHandler(IInventoryCountRepository countRepository)
        => _countRepository = countRepository;

    public async Task<PagedResult<InventoryCountLineDto>> Handle(
        GetInventoryCountLinesQuery request, CancellationToken ct)
    {
        _ = await _countRepository.GetHeaderAsync(request.CountId, ct)
            ?? throw new NotFoundException("InventoryCount", request.CountId);

        var (page, pageSize) = Pagination.Normalize(request.Page, request.PageSize);
        var (lines, total) = await _countRepository.GetLinesPagedAsync(request.CountId, page, pageSize, ct);

        var dtos = lines
            .Select(l => new InventoryCountLineDto(
                l.ItemId,
                l.Item.Name,
                l.Item.Category.Name,
                l.ExpectedQty,
                l.ActualQty))
            .ToList();

        return new PagedResult<InventoryCountLineDto>(dtos, page, pageSize, total);
    }
}
