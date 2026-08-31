using MediatR;
using POS.Application.Common.Interfaces;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Shifts.Commands.OpenShift;

public class OpenShiftCommandHandler : IRequestHandler<OpenShiftCommand, Guid>
{
    private readonly IShiftRepository _shifts;
    private readonly IBusinessDayRepository _days;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public OpenShiftCommandHandler(
        IShiftRepository shifts,
        IBusinessDayRepository days,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _shifts = shifts;
        _days = days;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(OpenShiftCommand request, CancellationToken ct)
    {
        var open = await _shifts.GetOpenAsync(ct);
        if (open is not null)
            throw new DomainException(
                $"Shift #{open.Number} is still open — close it with an X read before opening a new one.");

        var day = await _days.GetOpenAsync(ct);
        if (day is null)
        {
            var (recent, _) = await _days.GetPagedAsync(1, 1, ct);
            var lastDay = recent.FirstOrDefault();
            if (lastDay is not null
                && lastDay.OpenedAt.ToLocalTime().Date == DateTime.UtcNow.ToLocalTime().Date)
                throw new DomainException(
                    $"Day #{lastDay.Number} is already closed — the next business day opens after midnight.");

            day = new BusinessDay
            {
                Number = await _days.GetLastNumberAsync(ct) + 1,
                Status = DayStatus.Open,
                OpenedAt = DateTime.UtcNow,
                OpenedBy = _currentUser.Id
            };
            await _days.AddAsync(day, ct);
        }

        var shift = new Shift
        {
            Number = await _shifts.GetLastNumberAsync(ct) + 1,
            Status = ShiftStatus.Open,
            StartingCash = request.StartingCash,
            OpenedAt = DateTime.UtcNow,
            OpenedBy = _currentUser.Id,
            BusinessDayId = day.Id
        };

        await _shifts.AddAsync(shift, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return shift.Id;
    }
}
