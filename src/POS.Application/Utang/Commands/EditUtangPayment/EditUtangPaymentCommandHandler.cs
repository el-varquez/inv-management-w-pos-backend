using MediatR;
using POS.Application.Common.Interfaces;
using POS.Domain.Enums;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Utang.Commands.EditUtangPayment;

public class EditUtangPaymentCommandHandler : IRequestHandler<EditUtangPaymentCommand>
{
    private readonly IUtangRepository _utang;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public EditUtangPaymentCommandHandler(
        IUtangRepository utang, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _utang = utang;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(EditUtangPaymentCommand request, CancellationToken ct)
    {
        var entry = await _utang.GetEntryByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Utang entry", request.Id);

        if (entry.Type == UtangEntryType.Charge)
            throw new DomainException(
                "Charges mirror their sale and are never edited — void the sale instead.");

        if (entry.IsVoided)
            throw new DomainException("A voided payment can't be edited.");

        entry.EditedFrom ??= entry.Amount;
        entry.Amount = request.Amount;
        entry.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
