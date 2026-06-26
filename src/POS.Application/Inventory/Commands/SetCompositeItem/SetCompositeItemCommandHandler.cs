using MediatR;
using POS.Domain.Entities;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Inventory.Commands.SetCompositeItem;

public class SetCompositeItemCommandHandler : IRequestHandler<SetCompositeItemCommand>
{
    private readonly IItemRepository _itemRepository;
    private readonly ICompositeItemRepository _compositeItemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetCompositeItemCommandHandler(
        IItemRepository itemRepository,
        ICompositeItemRepository compositeItemRepository,
        IUnitOfWork unitOfWork)
    {
        _itemRepository = itemRepository;
        _compositeItemRepository = compositeItemRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(SetCompositeItemCommand request, CancellationToken ct)
    {
        var parent = await _itemRepository.GetByIdAsync(request.ParentItemId, ct)
            ?? throw new NotFoundException("Item", request.ParentItemId);

        await _compositeItemRepository.DeleteByParentIdAsync(request.ParentItemId, ct);

        foreach (var input in request.Components)
        {
            var component = await _itemRepository.GetByIdAsync(input.ComponentItemId, ct)
                ?? throw new NotFoundException("Component Item", input.ComponentItemId);

            if (input.ComponentItemId == request.ParentItemId)
                throw new DomainException("An item cannot be a component of itself.");

            await _compositeItemRepository.AddAsync(new CompositeItem
            {
                ParentItemId = request.ParentItemId,
                ComponentItemId = input.ComponentItemId,
                Quantity = input.Quantity
            }, ct);
        }

        parent.IsComposite = request.Components.Any();
        parent.UpdatedAt = DateTime.UtcNow;
        await _itemRepository.UpdateAsync(parent, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}