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
        var email = request.Email.Trim().ToLower();

        if (await _userRepository.GetByEmailAsync(email, ct) is not null)
            throw new DomainException("An account with this email already exists.");

        var cashier = new User
        {
            Name = request.Name.Trim(),
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = "Cashier",
            IsActive = true
        };

        await _userRepository.AddAsync(cashier, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return cashier.Id;
    }
}
