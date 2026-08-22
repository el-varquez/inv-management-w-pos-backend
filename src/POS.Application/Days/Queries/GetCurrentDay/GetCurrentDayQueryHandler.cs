using MediatR;
using POS.Application.Common;
using POS.Domain.Interfaces;

namespace POS.Application.Days.Queries.GetCurrentDay;

public class GetCurrentDayQueryHandler : IRequestHandler<GetCurrentDayQuery, DayReadDto?>
{
    private readonly IBusinessDayRepository _days;
    private readonly IShiftRepository _shifts;
    private readonly ITransactionRepository _transactions;

    public GetCurrentDayQueryHandler(
        IBusinessDayRepository days,
        IShiftRepository shifts,
        ITransactionRepository transactions)
    {
        _days = days;
        _shifts = shifts;
        _transactions = transactions;
    }

    public async Task<DayReadDto?> Handle(GetCurrentDayQuery request, CancellationToken ct)
    {
        var day = await _days.GetOpenAsync(ct);
        if (day is null) return null;

        var shifts = await _days.GetShiftsAsync(day.Id, ct);
        var reads = new List<ShiftReadDto>();
        foreach (var shift in shifts)
        {
            var transactions = await _transactions.GetByShiftAsync(shift.Id, ct);
            var movements = await _shifts.GetMovementsAsync(shift.Id, ct);
            var eWalletTransactions = await _shifts.GetEWalletTransactionsAsync(shift.Id, ct);
            reads.Add(ShiftRead.Build(shift, transactions, movements, eWalletTransactions));
        }

        return DayRead.Build(day, reads);
    }
}
