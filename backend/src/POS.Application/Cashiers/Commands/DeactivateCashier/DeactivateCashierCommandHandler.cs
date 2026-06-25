using MediatR;
using POS.Application.Common.Interfaces;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Cashiers.Commands.DeactivateCashier;

public class DeactivateCashierCommandHandler : IRequestHandler<DeactivateCashierCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public DeactivateCashierCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(DeactivateCashierCommand request, CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId
            ?? throw new DomainException("No tenant context for the current user.");

        var cashier = await _userRepository.GetCashierByIdAsync(request.Id, tenantId, ct)
            ?? throw new NotFoundException("Cashier", request.Id);

        cashier.IsActive = false;
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
