using MediatR;
using POS.Application.Common.Interfaces;
using POS.Domain.Entities;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Auth.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResult>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterCommandHandler(
        ITenantRepository tenantRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IJwtService jwtService,
        IPasswordHasher passwordHasher)
    {
        _tenantRepository = tenantRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
    }

    public async Task<RegisterResult> Handle(RegisterCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLower();

        if (await _userRepository.GetByEmailAsync(email, ct) is not null)
            throw new DomainException("An account with this email already exists.");

        var tenant = new Tenant
        {
            Name = request.BusinessName.Trim(),
            CashierCap = 5
        };
        await _tenantRepository.AddAsync(tenant, ct);

        var admin = new User
        {
            Name = request.AdminName.Trim(),
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = "Admin",
            IsActive = true,
            TenantId = tenant.Id
        };
        await _userRepository.AddAsync(admin, ct);

        await _unitOfWork.SaveChangesAsync(ct);

        var token = _jwtService.GenerateToken(admin);
        return new RegisterResult(token, admin.Name, admin.Email, admin.Role);
    }
}
