using MediatR;

namespace POS.Application.Shifts.Commands.CorrectShiftCount;

public record CorrectShiftCountCommand(
    Guid ShiftId,
    decimal CountedCash,
    string Reason
) : IRequest;
