using MediatR;
using POS.Domain.Enums;

namespace POS.Application.Shifts.Commands.RecordEWalletTransaction;

public record RecordEWalletTransactionCommand(
    EWalletDirection Direction,
    decimal Principal,
    decimal Fee
) : IRequest<Guid>;
