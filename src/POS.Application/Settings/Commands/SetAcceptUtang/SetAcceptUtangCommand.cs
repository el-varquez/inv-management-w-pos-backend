using MediatR;

namespace POS.Application.Settings.Commands.SetAcceptUtang;

public record SetAcceptUtangCommand(
    bool Accept,
    string? Username = null,
    string? Password = null
) : IRequest;
