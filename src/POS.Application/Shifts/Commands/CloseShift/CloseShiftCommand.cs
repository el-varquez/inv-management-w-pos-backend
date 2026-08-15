using MediatR;

namespace POS.Application.Shifts.Commands.CloseShift;

public record CloseShiftCommand(
    Guid ShiftId,
    decimal CountedCash,
    decimal? CountedEWalletBalance
) : IRequest;
