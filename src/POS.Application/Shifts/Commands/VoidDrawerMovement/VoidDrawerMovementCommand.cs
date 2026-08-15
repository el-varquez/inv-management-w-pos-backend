using MediatR;

namespace POS.Application.Shifts.Commands.VoidDrawerMovement;

public record VoidDrawerMovementCommand(Guid Id) : IRequest;
