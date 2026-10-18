using MediatR;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Inventory.Queries.GetInventoryCount;

public class GetInventoryCountQueryHandler
    : IRequestHandler<GetInventoryCountQuery, InventoryCountDto>
{
    private readonly IInventoryCountRepository _countRepository;

    public GetInventoryCountQueryHandler(IInventoryCountRepository countRepository)
        => _countRepository = countRepository;

    public async Task<InventoryCountDto> Handle(GetInventoryCountQuery request, CancellationToken ct)
    {
        var count = await _countRepository.GetHeaderAsync(request.Id, ct)
            ?? throw new NotFoundException("InventoryCount", request.Id);

        var (lineCount, countedCount, varianceCount) =
            await _countRepository.GetLineStatsAsync(count.Id, ct);

        return new InventoryCountDto(
            count.Id,
            count.Reference,
            count.Notes,
            count.Status.ToString(),
            count.CreatedAt,
            count.CompletedAt,
            lineCount,
            countedCount,
            varianceCount);
    }
}
