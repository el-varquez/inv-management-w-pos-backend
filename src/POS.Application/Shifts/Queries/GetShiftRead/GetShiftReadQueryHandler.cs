using MediatR;
using POS.Application.Common;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Shifts.Queries.GetShiftRead;

public class GetShiftReadQueryHandler : IRequestHandler<GetShiftReadQuery, ShiftReadDto>
{
    private readonly IShiftRepository _shifts;
    private readonly ITransactionRepository _transactions;
    private readonly IUtangRepository _utang;

    public GetShiftReadQueryHandler(
        IShiftRepository shifts,
        ITransactionRepository transactions,
        IUtangRepository utang)
    {
        _shifts = shifts;
        _transactions = transactions;
        _utang = utang;
    }

    public async Task<ShiftReadDto> Handle(GetShiftReadQuery request, CancellationToken ct)
    {
        var shift = await _shifts.GetByIdAsync(request.ShiftId, ct)
            ?? throw new NotFoundException("Shift", request.ShiftId);

        var transactions = await _transactions.GetByShiftAsync(shift.Id, ct);
        var movements = await _shifts.GetMovementsAsync(shift.Id, ct);
        var eWalletTransactions = await _shifts.GetEWalletTransactionsAsync(shift.Id, ct);
        var utangCharges = await _utang.GetChargesByShiftAsync(shift.Id, ct);
        var utangPayments = await _utang.GetPaymentsByShiftAsync(shift.Id, ct);

        return ShiftRead.Build(
            shift, transactions, movements, eWalletTransactions, utangCharges, utangPayments);
    }
}
