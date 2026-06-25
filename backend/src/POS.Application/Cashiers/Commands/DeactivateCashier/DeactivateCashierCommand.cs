using MediatR;

namespace POS.Application.Cashiers.Commands.DeactivateCashier;

public record DeactivateCashierCommand(Guid Id) : IRequest;
