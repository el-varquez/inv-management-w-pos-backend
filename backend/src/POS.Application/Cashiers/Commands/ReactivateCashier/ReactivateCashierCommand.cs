using MediatR;

namespace POS.Application.Cashiers.Commands.ReactivateCashier;

public record ReactivateCashierCommand(Guid Id) : IRequest;
