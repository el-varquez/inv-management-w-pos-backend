using MediatR;
using POS.Application.Common.Interfaces;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Cashiers.Commands.ResetCashierPassword;

public class ResetCashierPasswordCommandHandler : IRequestHandler<ResetCashierPasswordCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUser _currentUser;

    public ResetCashierPasswordCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ICurrentUser currentUser)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
    }

    public async Task Handle(ResetCashierPasswordCommand request, CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId
            ?? throw new DomainException("No tenant context for the current user.");

        var cashier = await _userRepository.GetCashierByIdAsync(request.Id, tenantId, ct)
            ?? throw new NotFoundException("Cashier", request.Id);

        cashier.PasswordHash = _passwordHasher.Hash(request.Password);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
