using MediatR;
using POS.Application.Common.Interfaces;
using POS.Domain.Enums;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Shifts.Commands.CorrectShiftCount;

public class CorrectShiftCountCommandHandler
    : IRequestHandler<CorrectShiftCountCommand>
{
    private readonly IShiftRepository _shifts;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CorrectShiftCountCommandHandler(
        IShiftRepository shifts,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _shifts = shifts;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(CorrectShiftCountCommand request, CancellationToken ct)
    {
        var shift = await _shifts.GetByIdAsync(request.ShiftId, ct)
            ?? throw new NotFoundException("Shift", request.ShiftId);

        if (shift.Status != ShiftStatus.Closed || shift.Snapshot is null)
            throw new DomainException(
                $"Shift #{shift.Number} is still open — close it with a Z read first.");

        var snapshot = shift.Snapshot;
        snapshot.CountedCashOriginal ??= snapshot.CountedCash;
        snapshot.CountedCash = request.CountedCash;
        snapshot.CashVariance = request.CountedCash - snapshot.ExpectedCash;
        snapshot.CorrectedAt = DateTime.UtcNow;
        snapshot.CorrectedBy = _currentUser.Id;
        snapshot.CorrectionReason = request.Reason.Trim();

        shift.UpdatedAt = DateTime.UtcNow;

        await _shifts.UpdateAsync(shift, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
