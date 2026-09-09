using MediatR;
using POS.Application.Common;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Days.Queries.GetDayRead;

public class GetDayReadQueryHandler : IRequestHandler<GetDayReadQuery, DayReadDto>
{
    private readonly IBusinessDayRepository _days;
    private readonly IShiftRepository _shifts;
    private readonly ISaleRepository _sales;
    private readonly IPaymentMethodRepository _methods;

    public GetDayReadQueryHandler(
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

    public async Task<DayReadDto> Handle(GetDayReadQuery request, CancellationToken ct)
    {
        var day = await _days.GetByIdAsync(request.DayId, ct)
            ?? throw new NotFoundException("Day", request.DayId);

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
