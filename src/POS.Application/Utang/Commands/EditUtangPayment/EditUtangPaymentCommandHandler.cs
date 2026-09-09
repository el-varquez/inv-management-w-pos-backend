using MediatR;
using POS.Application.Common;
using POS.Application.Common.Interfaces;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Utang.Commands.EditUtangPayment;

public class EditUtangPaymentCommandHandler : IRequestHandler<EditUtangPaymentCommand>
{
    private readonly IUtangRepository _utang;
    private readonly IStoreSettingsRepository _settings;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public EditUtangPaymentCommandHandler(
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

    public async Task Handle(EditUtangPaymentCommand request, CancellationToken ct)
    {
        await UtangGate.RequireOnAsync(_settings, ct);

        var payment = await _utang.GetPaymentByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Payment", request.Id);

        if (payment.IsVoided)
            throw new DomainException("A voided payment can't be edited.");

        payment.EditedFrom ??= payment.Amount;
        payment.Amount = request.Amount;
        payment.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
