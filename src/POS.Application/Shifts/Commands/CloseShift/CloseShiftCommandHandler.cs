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
    private readonly IUtangRepository _utang;
    private readonly IPaymentMethodRepository _methods;

    public CloseShiftCommandHandler(
        IShiftRepository shifts,
        ITransactionRepository transactions,
        IStoreSettingsRepository settings,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IUtangRepository utang,
        IPaymentMethodRepository methods)
    {
        _shifts = shifts;
        _transactions = transactions;
        _settings = settings;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _utang = utang;
        _methods = methods;
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
        var eWalletTransactions = await _shifts.GetEWalletTransactionsAsync(shift.Id, ct);
        var wallet = EWalletTotals.Of(eWalletTransactions);
        var utangCharges = await _utang.GetChargesByShiftAsync(shift.Id, ct);
        var utangPayments = await _utang.GetPaymentsByShiftAsync(shift.Id, ct);
        var utang = UtangTotals.Of(utangCharges, utangPayments);
        var methods = await _methods.GetAllAsync(ct);

        var movementsNet = movements.Where(m => !m.IsVoided).Sum(m => m.Amount);
        var methodSales = methods
            .Where(m => m.Type == PaymentMethodType.Sales)
            .Where(m => m.IsActive || transactions.Any(t => t.PaymentMethodId == m.Id))
            .Select(m => new MethodSalesDto(m.Id, m.Name,
                PaidSales.Net(transactions.Where(t => t.PaymentMethodId == m.Id))))
            .ToList();
        var cashSales = methodSales
            .FirstOrDefault(m => m.PaymentMethodId == PaymentMethodIds.Cash)?.Amount ?? 0m;
        var eWalletSales = methodSales
            .FirstOrDefault(m => m.PaymentMethodId == PaymentMethodIds.EWallet)?.Amount ?? 0m;
        var expectedCash = shift.StartingCash + cashSales + movementsNet
            + wallet.DrawerNet + utang.Collections;

        var closedAt = DateTime.UtcNow;

        var snapshot = new XReadSnapshot
        {
            NetSales = PaidSales.Net(transactions),
            TransactionCount = PaidSales.Count(transactions),
            EWalletCashInCount = wallet.CashInCount,
            EWalletCashIn = wallet.CashIn,
            EWalletCashOutCount = wallet.CashOutCount,
            EWalletCashOut = wallet.CashOut,
            UtangChargedCount = utang.ChargeCount,
            UtangCharged = utang.Charged,
            UtangMarkup = utang.Markup,
            UtangCollections = utang.Collections,
            Refunds = PaidSales.Refunds(transactions),
            RefundCount = PaidSales.RefundCount(transactions),
            DrawerMovementsNet = movementsNet,
            ExpectedCash = expectedCash,
            CountedCash = request.CountedCash,
            CashVariance = request.CountedCash - expectedCash
        };

        if (trackWallet)
        {
            var expectedWallet =
                (shift.StartingEWalletBalance ?? 0m) + eWalletSales + wallet.WalletNet;
            snapshot.ExpectedEWalletBalance = expectedWallet;
            snapshot.CountedEWalletBalance = request.CountedEWalletBalance;
            snapshot.EWalletVariance = request.CountedEWalletBalance - expectedWallet;
        }

        shift.Snapshot = snapshot;
        shift.Status = ShiftStatus.Closed;
        shift.ClosedAt = closedAt;
        shift.ClosedBy = _currentUser.Id;
        shift.UpdatedAt = closedAt;
        var methodSalesRows = methodSales
            .Select(m => new ShiftMethodSales
            {
                ShiftId = shift.Id,
                PaymentMethodId = m.PaymentMethodId,
                MethodName = m.Name,
                Amount = m.Amount
            })
            .ToList();

        await _shifts.UpdateAsync(shift, ct);
        await _shifts.AddMethodSalesAsync(methodSalesRows, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
