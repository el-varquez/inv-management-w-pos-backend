using MediatR;

namespace POS.Application.Shifts.Commands.RecordDrawerMovement;

public record RecordDrawerMovementCommand(
    decimal Amount,
    string Note
) : IRequest<Guid>;
