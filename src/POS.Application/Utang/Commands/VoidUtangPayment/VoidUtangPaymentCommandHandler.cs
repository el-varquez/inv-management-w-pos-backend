using MediatR;
using POS.Application.Common.Interfaces;
using POS.Domain.Enums;
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
        var entry = await _utang.GetEntryByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Utang entry", request.Id);

        if (entry.Type == UtangEntryType.Charge)
            throw new DomainException(
                "A charge is voided by refunding its sale — refund the receipt instead.");

        if (entry.IsVoided)
            throw new DomainException("This payment is already voided.");

        entry.IsVoided = true;
        entry.VoidedAt = DateTime.UtcNow;
        entry.VoidedBy = _currentUser.Id;

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
