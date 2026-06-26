using MediatR;

namespace POS.Application.Inventory.Commands.CreateInventoryCount;

public record CreateInventoryCountCommand(
    string? Notes
) : IRequest<Guid>;