using MediatR;

namespace POS.Application.Auth.Commands.Login;

public record LoginCommand(
    string Email,
    string Password
) : IRequest<LoginResult>;

public record LoginResult(
    string Token,
    string Name,
    string Email,
    string Role
);