using MediatR;
using POS.Application.Common;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Days.Queries.GetDayRead;

public class GetDayReadQueryHandler : IRequestHandler<GetDayReadQuery, DayReadDto>
{
    private readonly IBusinessDayRepository _days;
    private readonly IShiftRepository _shifts;
    private readonly ITransactionRepository _transactions;

    public GetDayReadQueryHandler(
        IBusinessDayRepository days,
        IShiftRepository shifts,
        ITransactionRepository transactions)
    {
        _days = days;
        _shifts = shifts;
        _transactions = transactions;
    }

    public async Task<DayReadDto> Handle(GetDayReadQuery request, CancellationToken ct)
    {
        var day = await _days.GetByIdAsync(request.DayId, ct)
            ?? throw new NotFoundException("Day", request.DayId);

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
