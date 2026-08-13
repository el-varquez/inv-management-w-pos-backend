using MediatR;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Cashiers.Commands.ReactivateCashier;

public class ReactivateCashierCommandHandler : IRequestHandler<ReactivateCashierCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ReactivateCashierCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ReactivateCashierCommand request, CancellationToken ct)
    {
        var cashier = await _userRepository.GetCashierByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Cashier", request.Id);

        if (cashier.IsActive)
            return;

        cashier.IsActive = true;
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
