using MediatR;
using POS.Domain.Entities;

namespace POS.Application.Auth.Commands.Login;

public record LoginCommand(
    string Username,
    string Password
) : IRequest<LoginResult>;

public record LoginResult(User? User, bool PasswordSetupRequired);
