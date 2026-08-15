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
    private readonly IStoreSettingsRepository _settings;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public OpenShiftCommandHandler(
        IShiftRepository shifts,
        IStoreSettingsRepository settings,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _shifts = shifts;
        _settings = settings;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(OpenShiftCommand request, CancellationToken ct)
    {
        var open = await _shifts.GetOpenAsync(ct);
        if (open is not null)
            throw new DomainException(
                $"Shift #{open.Number} is still open — close it with a Z read before opening a new one.");

        var settings = await _settings.GetAsync(ct);
        if (settings?.TrackEWalletFloat == true && request.StartingEWalletBalance is null)
            throw new DomainException(
                "Declare the starting e-wallet balance to open the shift.");

        var shift = new Shift
        {
            Number = await _shifts.GetLastNumberAsync(ct) + 1,
            Status = ShiftStatus.Open,
            StartingCash = request.StartingCash,
            StartingEWalletBalance = request.StartingEWalletBalance,
            OpenedAt = DateTime.UtcNow,
            OpenedBy = _currentUser.Id
        };

        await _shifts.AddAsync(shift, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return shift.Id;
    }
}
