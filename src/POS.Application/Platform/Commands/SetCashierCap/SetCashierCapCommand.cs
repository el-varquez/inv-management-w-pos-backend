using MediatR;

namespace POS.Application.Platform.Commands.SetCashierCap;

public record SetCashierCapCommand(Guid Id, int CashierCap) : IRequest;
