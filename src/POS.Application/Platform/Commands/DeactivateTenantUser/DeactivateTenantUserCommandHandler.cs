using MediatR;
using POS.Application.Common.Interfaces;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Platform.Commands.DeactivateTenantUser;

public class DeactivateTenantUserCommandHandler : IRequestHandler<DeactivateTenantUserCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateTenantUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeactivateTenantUserCommand request, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdInTenantAsync(request.UserId, request.TenantId, ct)
            ?? throw new NotFoundException("User", request.UserId);

        if (user.Role != "Cashier")
            throw new DomainException("Only cashier accounts can be deactivated.");

        user.IsActive = false;
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
