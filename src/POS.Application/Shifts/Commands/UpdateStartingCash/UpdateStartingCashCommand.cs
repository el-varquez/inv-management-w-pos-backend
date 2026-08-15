using MediatR;

namespace POS.Application.Shifts.Commands.UpdateStartingCash;

public record UpdateStartingCashCommand(
    Guid ShiftId,
    decimal StartingCash,
    string Reason
) : IRequest;
