using MediatR;
using POS.Application.Common;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Shifts.Queries.GetShiftRead;

public class GetShiftReadQueryHandler : IRequestHandler<GetShiftReadQuery, ShiftReadDto>
{
    private readonly IShiftRepository _shifts;
    private readonly ISaleRepository _sales;
    private readonly IPaymentMethodRepository _methods;

    public GetShiftReadQueryHandler(
        IShiftRepository shifts,
        ISaleRepository sales,
        IPaymentMethodRepository methods)
    {
        _shifts = shifts;
        _sales = sales;
        _methods = methods;
    }

    public async Task<ShiftReadDto> Handle(GetShiftReadQuery request, CancellationToken ct)
    {
        var shift = await _shifts.GetByIdAsync(request.ShiftId, ct)
            ?? throw new NotFoundException("Shift", request.ShiftId);

        var sales = await _sales.GetByShiftAsync(shift.Id, ct);
        var movements = await _shifts.GetMovementsAsync(shift.Id, ct);
        var methods = await _methods.GetAllAsync(ct);

        return ShiftRead.Build(
            shift, sales, movements, methods);
    }
}
