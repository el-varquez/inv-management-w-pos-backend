using MediatR;

namespace POS.Application.Cashiers.Commands.ResetCashierPassword;

public record ResetCashierPasswordCommand(Guid Id, string Password) : IRequest;
