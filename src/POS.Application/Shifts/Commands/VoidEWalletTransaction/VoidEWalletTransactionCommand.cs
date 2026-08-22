using MediatR;

namespace POS.Application.Shifts.Commands.VoidEWalletTransaction;

public record VoidEWalletTransactionCommand(Guid Id) : IRequest;
