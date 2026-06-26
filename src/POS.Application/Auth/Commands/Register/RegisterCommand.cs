using MediatR;

namespace POS.Application.Auth.Commands.Register;

public record RegisterCommand(
    string BusinessName,
    string AdminName,
    string Email,
    string Password
) : IRequest<RegisterResult>;

public record RegisterResult(
    string Token,
    string Name,
    string Email,
    string Role
);
