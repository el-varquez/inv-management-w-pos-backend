using MediatR;
using POS.Application.Common.Interfaces;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Utang.Commands.VoidUtangPayment;

public class VoidUtangPaymentCommandHandler : IRequestHandler<VoidUtangPaymentCommand>
{
    private readonly IUtangRepository _utang;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public VoidUtangPaymentCommandHandler(
        IUtangRepository utang, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _utang = utang;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(VoidUtangPaymentCommand request, CancellationToken ct)
    {
        var payment = await _utang.GetPaymentByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Utang payment", request.Id);

        if (payment.IsVoided)
            throw new DomainException("This payment is already voided.");

        payment.IsVoided = true;
        payment.VoidedAt = DateTime.UtcNow;
        payment.VoidedBy = _currentUser.Id;

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
