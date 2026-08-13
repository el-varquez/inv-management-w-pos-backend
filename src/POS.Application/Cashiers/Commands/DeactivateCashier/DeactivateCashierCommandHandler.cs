using MediatR;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Cashiers.Commands.DeactivateCashier;

public class DeactivateCashierCommandHandler : IRequestHandler<DeactivateCashierCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateCashierCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeactivateCashierCommand request, CancellationToken ct)
    {
        var cashier = await _userRepository.GetCashierByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Cashier", request.Id);

        cashier.IsActive = false;
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
