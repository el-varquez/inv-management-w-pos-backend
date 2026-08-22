using MediatR;
using POS.Application.Common;
using POS.Domain.Interfaces;

namespace POS.Application.Shifts.Queries.GetCurrentShift;

public class GetCurrentShiftQueryHandler : IRequestHandler<GetCurrentShiftQuery, ShiftReadDto?>
{
    private readonly IShiftRepository _shifts;
    private readonly ITransactionRepository _transactions;
    private readonly IUtangRepository _utang;

    public GetCurrentShiftQueryHandler(
        IShiftRepository shifts,
        ITransactionRepository transactions,
        IUtangRepository utang)
    {
        _shifts = shifts;
        _transactions = transactions;
        _utang = utang;
    }

    public async Task<ShiftReadDto?> Handle(GetCurrentShiftQuery request, CancellationToken ct)
    {
        var shift = await _shifts.GetOpenAsync(ct);
        if (shift is null) return null;

        var transactions = await _transactions.GetByShiftAsync(shift.Id, ct);
        var movements = await _shifts.GetMovementsAsync(shift.Id, ct);
        var eWalletTransactions = await _shifts.GetEWalletTransactionsAsync(shift.Id, ct);
        var utangEntries = await _utang.GetEntriesByShiftAsync(shift.Id, ct);

        return ShiftRead.Build(shift, transactions, movements, eWalletTransactions, utangEntries);
    }
}
