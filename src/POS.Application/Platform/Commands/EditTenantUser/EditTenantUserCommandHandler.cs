using MediatR;
using POS.Application.Common.Interfaces;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Platform.Commands.EditTenantUser;

public class EditTenantUserCommandHandler : IRequestHandler<EditTenantUserCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public EditTenantUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task Handle(EditTenantUserCommand request, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdInTenantAsync(request.UserId, request.TenantId, ct)
            ?? throw new NotFoundException("User", request.UserId);

        var email = request.Email.Trim().ToLower();

        if (email != user.Email &&
            await _userRepository.GetByEmailAsync(email, ct) is not null)
            throw new DomainException("An account with this email already exists.");

        user.Name = request.Name.Trim();
        user.Email = email;

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            if (_passwordHasher.Verify(request.Password, user.PasswordHash))
                throw new DomainException(
                    "That's already this user's current password. Please choose a different one.");

            user.PasswordHash = _passwordHasher.Hash(request.Password);
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
