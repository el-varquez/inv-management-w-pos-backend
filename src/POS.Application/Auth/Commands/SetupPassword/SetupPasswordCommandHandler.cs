using MediatR;
using POS.Application.Auth.Commands.Login;
using POS.Application.Common.Interfaces;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Auth.Commands.SetupPassword;

public class SetupPasswordCommandHandler : IRequestHandler<SetupPasswordCommand, LoginResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public SetupPasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<LoginResult> Handle(SetupPasswordCommand request, CancellationToken ct)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email.Trim().ToLower(), ct)
            ?? throw new DomainException("Invalid email or password.");

        if (!user.IsActive)
            throw new DomainException("Account is inactive.");

        if (!string.IsNullOrEmpty(user.PasswordHash))
            throw new DomainException("A password is already set for this account.");

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        await _unitOfWork.SaveChangesAsync(ct);

        return new LoginResult(user, PasswordSetupRequired: false);
    }
}
