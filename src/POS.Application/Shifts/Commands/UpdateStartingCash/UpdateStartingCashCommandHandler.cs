using MediatR;
using POS.Application.Common.Interfaces;
using POS.Domain.Enums;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Shifts.Commands.UpdateStartingCash;

public class UpdateStartingCashCommandHandler
    : IRequestHandler<UpdateStartingCashCommand>
{
    private readonly IShiftRepository _shifts;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateStartingCashCommandHandler(
        IShiftRepository shifts,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _shifts = shifts;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateStartingCashCommand request, CancellationToken ct)
    {
        var shift = await _shifts.GetByIdAsync(request.ShiftId, ct)
            ?? throw new NotFoundException("Shift", request.ShiftId);

        if (shift.Status != ShiftStatus.Open)
            throw new DomainException(
                $"Shift #{shift.Number} is closed — starting cash can no longer be changed.");

        shift.StartingCashOriginal ??= shift.StartingCash;
        shift.StartingCash = request.StartingCash;
        shift.StartingCashCorrectedAt = DateTime.UtcNow;
        shift.StartingCashCorrectedBy = _currentUser.Id;
        shift.StartingCashCorrectionReason = request.Reason.Trim();
        shift.UpdatedAt = DateTime.UtcNow;

        await _shifts.UpdateAsync(shift, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
