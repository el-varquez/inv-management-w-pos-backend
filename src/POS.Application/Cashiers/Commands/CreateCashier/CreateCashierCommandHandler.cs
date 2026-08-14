using MediatR;
using POS.Application.Common.Interfaces;
using POS.Domain.Entities;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Cashiers.Commands.CreateCashier;

public class CreateCashierCommandHandler : IRequestHandler<CreateCashierCommand, Guid>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public CreateCashierCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<Guid> Handle(CreateCashierCommand request, CancellationToken ct)
    {
        var username = request.Username.Trim().ToLower();

        if (await _userRepository.GetByUsernameAsync(username, ct) is not null)
            throw new DomainException("An account with this username already exists.");

        var email = request.Email?.Trim();

        var cashier = new User
        {
            Name = request.Name.Trim(),
            Username = username,
            Email = string.IsNullOrWhiteSpace(email) ? null : email.ToLower(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = "Cashier",
            IsActive = true
        };

        await _userRepository.AddAsync(cashier, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return cashier.Id;
    }
}
