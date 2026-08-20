using MediatR;

namespace POS.Application.Days.Commands.ReopenDay;

public record ReopenDayCommand(
    Guid DayId,
    string Username,
    string Password,
    string Reason
) : IRequest;
