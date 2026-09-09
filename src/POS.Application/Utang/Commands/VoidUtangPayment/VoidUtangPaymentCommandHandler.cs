using MediatR;
using POS.Application.Common;
using POS.Application.Common.Interfaces;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Utang.Commands.VoidUtangPayment;

public class VoidUtangPaymentCommandHandler : IRequestHandler<VoidUtangPaymentCommand>
{
    private readonly IUtangRepository _utang;
    private readonly IStoreSettingsRepository _settings;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public VoidUtangPaymentCommandHandler(
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

    public async Task Handle(VoidUtangPaymentCommand request, CancellationToken ct)
    {
        await UtangGate.RequireOnAsync(_settings, ct);

        var payment = await _utang.GetPaymentByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Payment", request.Id);

        if (payment.IsVoided)
            throw new DomainException("This payment is already voided.");

        payment.IsVoided = true;
        payment.VoidedAt = DateTime.UtcNow;
        payment.VoidedBy = _currentUser.Id;

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
