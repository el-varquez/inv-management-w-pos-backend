using MediatR;
using POS.Application.Common.Interfaces;
using POS.Domain.Entities;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Shifts.Commands.RecordDrawerMovement;

public class RecordDrawerMovementCommandHandler
    : IRequestHandler<RecordDrawerMovementCommand, Guid>
{
    private readonly IShiftRepository _shifts;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public RecordDrawerMovementCommandHandler(
        IShiftRepository shifts,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _shifts = shifts;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(RecordDrawerMovementCommand request, CancellationToken ct)
    {
        var shift = await _shifts.GetOpenAsync(ct)
            ?? throw new DomainException(
                "No open shift — drawer movements belong to an open shift.");

        var movement = new CashDrawerMovement
        {
            ShiftId = shift.Id,
            Amount = request.Amount,
            Note = request.Note.Trim(),
            CreatedBy = _currentUser.Id
        };

        await _shifts.AddMovementAsync(movement, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return movement.Id;
    }
}
