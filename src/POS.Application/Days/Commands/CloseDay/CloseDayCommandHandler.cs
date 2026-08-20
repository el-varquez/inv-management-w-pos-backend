using MediatR;
using POS.Application.Common.Interfaces;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Days.Commands.CloseDay;

public class CloseDayCommandHandler : IRequestHandler<CloseDayCommand>
{
    private readonly IBusinessDayRepository _days;
    private readonly IShiftRepository _shifts;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CloseDayCommandHandler(
        IBusinessDayRepository days,
        IShiftRepository shifts,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _days = days;
        _shifts = shifts;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(CloseDayCommand request, CancellationToken ct)
    {
        var day = await _days.GetOpenAsync(ct)
            ?? throw new DomainException("No day is open — nothing to close.");

        var open = await _shifts.GetOpenAsync(ct);
        if (open is not null)
            throw new DomainException(
                $"Shift #{open.Number} is still open — close it with an X read before closing the day.");

        var shifts = await _days.GetShiftsAsync(day.Id, ct);
        var reads = shifts
            .Where(s => s.Snapshot is not null)
            .Select(s => s.Snapshot!)
            .ToList();
        var lastRead = shifts.LastOrDefault(s => s.Snapshot is not null)?.Snapshot;

        var snapshot = new ZReadSnapshot
        {
            NetSales = reads.Sum(x => x.NetSales),
            TransactionCount = reads.Sum(x => x.TransactionCount),
            CashSales = reads.Sum(x => x.CashSales),
            GcashSales = reads.Sum(x => x.GcashSales),
            MayaSales = reads.Sum(x => x.MayaSales),
            DrawerMovementsNet = reads.Sum(x => x.DrawerMovementsNet),
            CashVariance = reads.Sum(x => x.CashVariance),
            CountedCash = lastRead?.CountedCash ?? 0m,
            CountedEWalletBalance = lastRead?.CountedEWalletBalance,
            EWalletVariance = reads.Any(x => x.EWalletVariance is not null)
                ? reads.Sum(x => x.EWalletVariance ?? 0m)
                : null,
            ShiftCount = shifts.Count
        };

        var closedAt = DateTime.UtcNow;
        day.Snapshot = snapshot;
        day.Status = DayStatus.Closed;
        day.ClosedAt = closedAt;
        day.ClosedBy = _currentUser.Id;
        day.ClosedLate = day.OpenedAt.ToLocalTime().Date < closedAt.ToLocalTime().Date;
        day.UpdatedAt = closedAt;

        await _days.UpdateAsync(day, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
