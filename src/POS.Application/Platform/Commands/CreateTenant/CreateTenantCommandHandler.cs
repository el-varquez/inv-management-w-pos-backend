using MediatR;
using POS.Application.Common.Interfaces;
using POS.Domain.Entities;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Platform.Commands.CreateTenant;

public class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, Guid>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public CreateTenantCommandHandler(
        ITenantRepository tenantRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher)
    {
        _tenantRepository = tenantRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<Guid> Handle(CreateTenantCommand request, CancellationToken ct)
    {
        var email = request.AdminEmail.Trim().ToLower();

        if (await _userRepository.GetByEmailAsync(email, ct) is not null)
            throw new DomainException("An account with this email already exists.");

        var tenant = new Tenant
        {
            Name = request.BusinessName.Trim(),
            CashierCap = request.CashierCap ?? 5,
            IsActive = true
        };
        await _tenantRepository.AddAsync(tenant, ct);

        var admin = new User
        {
            Name = request.AdminName.Trim(),
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.AdminPassword),
            Role = "Admin",
            IsActive = true,
            TenantId = tenant.Id
        };
        await _userRepository.AddAsync(admin, ct);

        await _unitOfWork.SaveChangesAsync(ct);

        return tenant.Id;
    }
}
