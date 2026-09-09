using MediatR;
using POS.Application.Common;
using POS.Domain.Interfaces;

namespace POS.Application.Days.Queries.GetCurrentDay;

public class GetCurrentDayQueryHandler : IRequestHandler<GetCurrentDayQuery, DayReadDto?>
{
    private readonly IBusinessDayRepository _days;
    private readonly IShiftRepository _shifts;
    private readonly ISaleRepository _sales;
    private readonly IPaymentMethodRepository _methods;

    public GetCurrentDayQueryHandler(
        IBusinessDayRepository days,
        IShiftRepository shifts,
        ISaleRepository sales,
        IPaymentMethodRepository methods)
    {
        _days = days;
        _shifts = shifts;
        _sales = sales;
        _methods = methods;
    }

    public async Task<DayReadDto?> Handle(GetCurrentDayQuery request, CancellationToken ct)
    {
        var day = await _days.GetOpenAsync(ct);
        if (day is null) return null;

        var shifts = await _days.GetShiftsAsync(day.Id, ct);
        var methods = await _methods.GetAllAsync(ct);
        var reads = new List<ShiftReadDto>();
        foreach (var shift in shifts)
        {
            var sales = await _sales.GetByShiftAsync(shift.Id, ct);
            var movements = await _shifts.GetMovementsAsync(shift.Id, ct);
            reads.Add(ShiftRead.Build(
                shift, sales, movements, methods));
        }

        return DayRead.Build(day, reads);
    }
}
