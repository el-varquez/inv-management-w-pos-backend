using MediatR;
using POS.Application.Common.Interfaces;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public LoginCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username.Trim().ToLower(), ct)
            ?? throw new DomainException("Invalid username or password.");

        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            if (!user.IsActive)
                throw new DomainException("Account is inactive.");
            return new LoginResult(user, PasswordSetupRequired: true);
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new DomainException("Invalid username or password.");

        if (!user.IsActive)
            throw new DomainException("Account is inactive.");

        return new LoginResult(user, PasswordSetupRequired: false);
    }
}
