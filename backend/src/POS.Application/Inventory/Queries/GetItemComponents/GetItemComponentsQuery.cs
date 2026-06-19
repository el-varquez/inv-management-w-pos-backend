using MediatR;

namespace POS.Application.Inventory.Queries.GetItemComponents;

public record ComponentDto(
    Guid ComponentItemId,
    string ComponentItemName,
    decimal Quantity,
    decimal ComponentCostPrice,
    decimal LineCost);

public record ItemComponentsDto(
    Guid ParentItemId,
    string ParentItemName,
    bool IsComposite,
    decimal TotalComponentCost,
    IList<ComponentDto> Components);

public record GetItemComponentsQuery(Guid ParentItemId)
    : IRequest<ItemComponentsDto>;
