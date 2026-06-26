using MediatR;

namespace POS.Application.Inventory.Commands.CompleteInventoryCount;

public record CountLineInput(Guid ItemId, int ActualQty);

public record CompleteInventoryCountCommand(
    Guid CountId,
    IList<CountLineInput> Lines
) : IRequest;