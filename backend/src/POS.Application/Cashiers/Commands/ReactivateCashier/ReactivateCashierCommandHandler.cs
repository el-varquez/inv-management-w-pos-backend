using MediatR;
using POS.Application.Common.Interfaces;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Cashiers.Commands.ReactivateCashier;

public class ReactivateCashierCommandHandler : IRequestHandler<ReactivateCashierCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public ReactivateCashierCommandHandler(
        IUserRepository userRepository,
        ITenantRepository tenantRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _userRepository = userRepository;
        _tenantRepository = tenantRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(ReactivateCashierCommand request, CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId
            ?? throw new DomainException("No tenant context for the current user.");

        var tenant = await _tenantRepository.GetByIdAsync(tenantId, ct)
            ?? throw new NotFoundException("Tenant", tenantId);

        var cashier = await _userRepository.GetCashierByIdAsync(request.Id, tenantId, ct)
            ?? throw new NotFoundException("Cashier", request.Id);

        if (cashier.IsActive)
            return;

        var activeCount = await _userRepository.CountActiveCashiersAsync(tenantId, ct);
        if (activeCount >= tenant.CashierCap)
            throw new DomainException(
                $"You've reached your cashier limit of {tenant.CashierCap}. " +
                "Deactivate a cashier to free up a slot.");

        cashier.IsActive = true;
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
