using MediatR;
using POS.Application.Common;
using POS.Application.Common.Interfaces;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Shifts.Commands.CloseShift;

public class CloseShiftCommandHandler : IRequestHandler<CloseShiftCommand>
{
    private readonly IShiftRepository _shifts;
    private readonly ITransactionRepository _transactions;
    private readonly IStoreSettingsRepository _settings;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CloseShiftCommandHandler(
        IShiftRepository shifts,
        ITransactionRepository transactions,
        IStoreSettingsRepository settings,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _shifts = shifts;
        _transactions = transactions;
        _settings = settings;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(CloseShiftCommand request, CancellationToken ct)
    {
        var shift = await _shifts.GetByIdAsync(request.ShiftId, ct)
            ?? throw new NotFoundException("Shift", request.ShiftId);

        if (shift.Status == ShiftStatus.Closed)
            throw new DomainException($"Shift #{shift.Number} is already closed.");

        var settings = await _settings.GetAsync(ct);
        var trackWallet = settings?.TrackEWalletFloat == true;

        if (trackWallet && request.CountedEWalletBalance is null)
            throw new DomainException(
                "Enter the counted e-wallet balance to close the shift.");

        var transactions = await _transactions.GetByShiftAsync(shift.Id, ct);
        var movements = await _shifts.GetMovementsAsync(shift.Id, ct);

        var movementsNet = movements.Where(m => !m.IsVoided).Sum(m => m.Amount);
        var cashSales = NetOf(transactions, PaymentType.Cash);
        var gcashSales = NetOf(transactions, PaymentType.Gcash);
        var expectedCash = shift.StartingCash + cashSales + movementsNet;

        var closedAt = DateTime.UtcNow;

        var snapshot = new XReadSnapshot
        {
            NetSales = PaidSales.Net(transactions),
            TransactionCount = PaidSales.Count(transactions),
            CashSales = cashSales,
            GcashSales = gcashSales,
            MayaSales = NetOf(transactions, PaymentType.Maya),
            Refunds = PaidSales.Refunds(transactions),
            RefundCount = PaidSales.RefundCount(transactions),
            DrawerMovementsNet = movementsNet,
            ExpectedCash = expectedCash,
            CountedCash = request.CountedCash,
            CashVariance = request.CountedCash - expectedCash
        };

        if (trackWallet)
        {
            var expectedWallet = (shift.StartingEWalletBalance ?? 0m) + gcashSales;
            snapshot.ExpectedEWalletBalance = expectedWallet;
            snapshot.CountedEWalletBalance = request.CountedEWalletBalance;
            snapshot.EWalletVariance = request.CountedEWalletBalance - expectedWallet;
        }

        shift.Snapshot = snapshot;
        shift.Status = ShiftStatus.Closed;
        shift.ClosedAt = closedAt;
        shift.ClosedBy = _currentUser.Id;
        shift.UpdatedAt = closedAt;

        await _shifts.UpdateAsync(shift, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private static decimal NetOf(IList<Transaction> transactions, PaymentType method)
        => PaidSales.Net(transactions.Where(t => t.PaymentType == method));
}
