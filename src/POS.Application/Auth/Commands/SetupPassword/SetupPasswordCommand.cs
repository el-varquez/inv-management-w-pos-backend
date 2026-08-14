using MediatR;
using POS.Application.Auth.Commands.Login;

namespace POS.Application.Auth.Commands.SetupPassword;

public record SetupPasswordCommand(
    string Username,
    string NewPassword
) : IRequest<LoginResult>;
