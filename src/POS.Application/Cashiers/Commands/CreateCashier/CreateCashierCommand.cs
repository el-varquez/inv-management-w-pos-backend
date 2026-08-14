using MediatR;

namespace POS.Application.Cashiers.Commands.CreateCashier;

public record CreateCashierCommand(
    string Name,
    string Username,
    string Password,
    string? Email = null
) : IRequest<Guid>;
