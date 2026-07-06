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
        var count = await _countRepository.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("InventoryCount", request.Id);

        var lines = count.Lines
            .OrderBy(l => l.Item.Name)
            .Select(l => new InventoryCountLineDto(
                l.ItemId,
                l.Item.Name,
                l.Item.Category.Name,
                l.ExpectedQty,
                l.ActualQty))
            .ToList();

        return new InventoryCountDto(
            count.Id,
            count.Reference,
            count.Notes,
            count.Status.ToString(),
            count.CreatedAt,
            count.CompletedAt,
            lines);
    }
}
