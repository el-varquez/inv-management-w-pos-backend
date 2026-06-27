using MediatR;
using POS.Application.Common.Interfaces;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Platform.Commands.ReactivateTenantUser;

public class ReactivateTenantUserCommandHandler : IRequestHandler<ReactivateTenantUserCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ReactivateTenantUserCommandHandler(
        IUserRepository userRepository,
        ITenantRepository tenantRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _tenantRepository = tenantRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ReactivateTenantUserCommand request, CancellationToken ct)
    {
        var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, ct)
            ?? throw new NotFoundException("Tenant", request.TenantId);

        var user = await _userRepository.GetByIdInTenantAsync(request.UserId, request.TenantId, ct)
            ?? throw new NotFoundException("User", request.UserId);

        if (user.Role != "Cashier")
            throw new DomainException("Only cashier accounts can be deactivated.");

        if (user.IsActive)
            return;

        var activeCount = await _userRepository.CountActiveCashiersAsync(request.TenantId, ct);
        if (activeCount >= tenant.CashierCap)
            throw new DomainException(
                $"This business has reached its cashier limit of {tenant.CashierCap}. " +
                "Deactivate a cashier or raise the cap to free up a slot.");

        user.IsActive = true;
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
