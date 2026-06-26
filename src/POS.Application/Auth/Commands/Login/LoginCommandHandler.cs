using MediatR;
using POS.Application.Common.Interfaces;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher _passwordHAsher;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IJwtService jwtService,
        IPasswordHasher passwordHasher
    )
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _passwordHAsher = passwordHasher;
    }

    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, ct)
            ?? throw new DomainException("Invalid email or password.");

        if (!_passwordHAsher.Verify(request.Password, user.PasswordHash))
            throw new DomainException("Invalid email or password.");

        if (!user.IsActive)
            throw new DomainException("Account is inactive.");

        var token = _jwtService.GenerateToken(user);

        return new LoginResult(token, user.Name, user.Email, user.Role);
    }
}