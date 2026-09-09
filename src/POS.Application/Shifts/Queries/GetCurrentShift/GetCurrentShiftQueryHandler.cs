using MediatR;
using POS.Application.Common;
using POS.Domain.Interfaces;

namespace POS.Application.Shifts.Queries.GetCurrentShift;

public class GetCurrentShiftQueryHandler : IRequestHandler<GetCurrentShiftQuery, ShiftReadDto?>
{
    private readonly IShiftRepository _shifts;
    private readonly ISaleRepository _sales;
    private readonly IPaymentMethodRepository _methods;

    public GetCurrentShiftQueryHandler(
        IShiftRepository shifts,
        ISaleRepository sales,
        IPaymentMethodRepository methods)
    {
        _shifts = shifts;
        _sales = sales;
        _methods = methods;
    }

    public async Task<ShiftReadDto?> Handle(GetCurrentShiftQuery request, CancellationToken ct)
    {
        var shift = await _shifts.GetOpenAsync(ct);
        if (shift is null) return null;

        var sales = await _sales.GetByShiftAsync(shift.Id, ct);
        var movements = await _shifts.GetMovementsAsync(shift.Id, ct);
        var methods = await _methods.GetAllAsync(ct);

        return ShiftRead.Build(
            shift, sales, movements, methods);
    }
}
