using MediatR;
using POS.Application.Common.Interfaces;
using POS.Domain.Entities;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Utang.Commands.CollectUtangPayment;

public class CollectUtangPaymentCommandHandler
    : IRequestHandler<CollectUtangPaymentCommand, Guid>
{
    private readonly IUtangRepository _utang;
    private readonly IShiftRepository _shifts;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CollectUtangPaymentCommandHandler(
        IUtangRepository utang,
        IShiftRepository shifts,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _utang = utang;
        _shifts = shifts;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(
        CollectUtangPaymentCommand request, CancellationToken ct)
    {
        var shift = await _shifts.GetOpenAsync(ct)
            ?? throw new DomainException(
                "No open shift — collections go into the drawer. Declare starting cash first.");

        var suki = await _utang.GetSukiByIdAsync(request.SukiId, ct)
            ?? throw new NotFoundException("Suki", request.SukiId);

        var balance = await _utang.GetBalanceAsync(suki.Id, ct);
        if (request.Amount > balance)
            throw new DomainException(
                $"That's more than {suki.Name} owes — the balance is ₱{balance:N2}.");

        var payment = new UtangPayment
        {
            SukiId = suki.Id,
            Amount = request.Amount,
            ShiftId = shift.Id,
            CreatedBy = _currentUser.Id
        };

        await _utang.AddPaymentAsync(payment, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return payment.Id;
    }
}
