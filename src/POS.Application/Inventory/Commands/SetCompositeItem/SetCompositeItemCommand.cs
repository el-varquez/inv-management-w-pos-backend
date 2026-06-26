using MediatR;

namespace POS.Application.Inventory.Commands.SetCompositeItem;

public record ComponentInput(Guid ComponentItemId, decimal Quantity);

public record SetCompositeItemCommand(
    Guid ParentItemId,
    IList<ComponentInput> Components
) : IRequest;