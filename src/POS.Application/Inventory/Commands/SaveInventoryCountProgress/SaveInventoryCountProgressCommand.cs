using MediatR;
using POS.Application.Inventory.Commands.CompleteInventoryCount;

namespace POS.Application.Inventory.Commands.SaveInventoryCountProgress;

public record SaveInventoryCountProgressCommand(
    Guid CountId,
    IList<CountLineInput> Lines
) : IRequest;
