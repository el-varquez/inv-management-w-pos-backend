using MediatR;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Inventory.Queries.GetItemComponents;

public class GetItemComponentsQueryHandler
    : IRequestHandler<GetItemComponentsQuery, ItemComponentsDto>
{
    private readonly IItemRepository _itemRepository;
    private readonly ICompositeItemRepository _compositeItemRepository;

    public GetItemComponentsQueryHandler(
        IItemRepository itemRepository,
        ICompositeItemRepository compositeItemRepository)
    {
        _itemRepository = itemRepository;
        _compositeItemRepository = compositeItemRepository;
    }

    public async Task<ItemComponentsDto> Handle(
        GetItemComponentsQuery request, CancellationToken ct)
    {
        var parent = await _itemRepository.GetByIdAsync(request.ParentItemId, ct)
            ?? throw new NotFoundException("Item", request.ParentItemId);

        var links = await _compositeItemRepository
            .GetByParentIdAsync(request.ParentItemId, ct);

        var components = links.Select(c => new ComponentDto(
            c.ComponentItemId,
            c.ComponentItem.Name,
            c.Quantity,
            c.ComponentItem.CostPrice,
            c.Quantity * c.ComponentItem.CostPrice
        )).ToList();

        return new ItemComponentsDto(
            parent.Id,
            parent.Name,
            parent.IsComposite,
            components.Sum(c => c.LineCost),
            components);
    }
}
