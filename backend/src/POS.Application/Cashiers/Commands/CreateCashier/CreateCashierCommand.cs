using MediatR;

namespace POS.Application.Cashiers.Commands.CreateCashier;

public record CreateCashierCommand(
    string Name,
    string Email,
    string Password
) : IRequest<Guid>;
