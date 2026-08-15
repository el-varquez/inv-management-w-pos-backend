using MediatR;
using POS.Application.Common.Interfaces;
using POS.Domain.Enums;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Shifts.Commands.VoidDrawerMovement;

public class VoidDrawerMovementCommandHandler : IRequestHandler<VoidDrawerMovementCommand>
{
    private readonly IShiftRepository _shifts;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public VoidDrawerMovementCommandHandler(
        IShiftRepository shifts,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _shifts = shifts;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(VoidDrawerMovementCommand request, CancellationToken ct)
    {
        var movement = await _shifts.GetMovementByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Drawer movement", request.Id);

        if (movement.IsVoided)
            throw new DomainException("This drawer movement is already voided.");

        var shift = await _shifts.GetByIdAsync(movement.ShiftId, ct);
        if (shift is null || shift.Status != ShiftStatus.Open)
            throw new DomainException(
                "The shift is closed — its drawer movements can no longer be changed.");

        movement.IsVoided = true;
        movement.VoidedAt = DateTime.UtcNow;
        movement.VoidedBy = _currentUser.Id;

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
