using MediatR;
using POS.Application.Common.Interfaces;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Utang.Commands.VoidUtangAdjustment;

public class VoidUtangAdjustmentCommandHandler
    : IRequestHandler<VoidUtangAdjustmentCommand>
{
    private readonly IUtangRepository _utang;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public VoidUtangAdjustmentCommandHandler(
        IUtangRepository utang, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _utang = utang;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(VoidUtangAdjustmentCommand request, CancellationToken ct)
    {
        var adjustment = await _utang.GetAdjustmentByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Utang adjustment", request.Id);

        if (adjustment.IsVoided)
            throw new DomainException("This adjustment is already voided.");

        adjustment.IsVoided = true;
        adjustment.VoidedAt = DateTime.UtcNow;
        adjustment.VoidedBy = _currentUser.Id;

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
