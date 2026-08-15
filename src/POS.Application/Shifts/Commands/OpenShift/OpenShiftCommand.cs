using MediatR;

namespace POS.Application.Shifts.Commands.OpenShift;

public record OpenShiftCommand(
    decimal StartingCash,
    decimal? StartingEWalletBalance
) : IRequest<Guid>;
