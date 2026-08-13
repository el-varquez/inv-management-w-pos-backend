using MediatR;
using POS.Domain.Entities;

namespace POS.Application.Auth.Commands.Login;

public record LoginCommand(
    string Email,
    string Password
) : IRequest<LoginResult>;

public record LoginResult(User? User, bool PasswordSetupRequired);
