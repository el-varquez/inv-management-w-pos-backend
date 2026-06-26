using MediatR;
using POS.Application.Common.Interfaces;
using POS.Domain.Entities;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Cashiers.Commands.CreateCashier;

public class CreateCashierCommandHandler : IRequestHandler<CreateCashierCommand, Guid>
{
    private readonly IUserRepository _userRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUser _currentUser;

    public CreateCashierCommandHandler(
        IUserRepository userRepository,
        ITenantRepository tenantRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ICurrentUser currentUser)
    {
        _userRepository = userRepository;
        _tenantRepository = tenantRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateCashierCommand request, CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId
            ?? throw new DomainException("No tenant context for the current user.");

        var tenant = await _tenantRepository.GetByIdAsync(tenantId, ct)
            ?? throw new NotFoundException("Tenant", tenantId);

        var email = request.Email.Trim().ToLower();

        if (await _userRepository.GetByEmailAsync(email, ct) is not null)
            throw new DomainException("An account with this email already exists.");

        var activeCount = await _userRepository.CountActiveCashiersAsync(tenantId, ct);
        if (activeCount >= tenant.CashierCap)
            throw new DomainException(
                $"You've reached your cashier limit of {tenant.CashierCap}. " +
                "Deactivate a cashier to free up a slot.");

        var cashier = new User
        {
            Name = request.Name.Trim(),
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = "Cashier",
            IsActive = true,
            TenantId = tenantId
        };

        await _userRepository.AddAsync(cashier, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return cashier.Id;
    }
}
