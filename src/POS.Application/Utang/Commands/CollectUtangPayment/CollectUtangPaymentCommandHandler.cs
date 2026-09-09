using MediatR;
using POS.Application.Common;
using POS.Application.Common.Interfaces;
using POS.Domain.Entities;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Utang.Commands.CollectUtangPayment;

public class CollectUtangPaymentCommandHandler
    : IRequestHandler<CollectUtangPaymentCommand, Guid>
{
    private readonly IUtangRepository _utang;
    private readonly IStoreSettingsRepository _settings;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CollectUtangPaymentCommandHandler(
        IUtangRepository utang,
        IStoreSettingsRepository settings,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _utang = utang;
        _settings = settings;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(
        CollectUtangPaymentCommand request, CancellationToken ct)
    {
        await UtangGate.RequireOnAsync(_settings, ct);

        var suki = await _utang.GetSukiByIdAsync(request.SukiId, ct)
            ?? throw new NotFoundException("Suki", request.SukiId);

        var balance = await _utang.GetBalanceAsync(suki.Id, ct);
        if (request.Amount > balance)
            throw new DomainException(
                $"That's more than {suki.Name} owes — the balance is ₱{balance:N2}.");

        var payment = new Payment
        {
            SukiId = suki.Id,
            Amount = request.Amount,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            CreatedBy = _currentUser.Id
        };

        await _utang.AddPaymentAsync(payment, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return payment.Id;
    }
}
