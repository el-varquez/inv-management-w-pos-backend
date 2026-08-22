using MediatR;
using POS.Application.Common.Interfaces;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Domain.Events;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Shifts.Commands.RecordEWalletTransaction;

public class RecordEWalletTransactionCommandHandler
    : IRequestHandler<RecordEWalletTransactionCommand, Guid>
{
    private readonly IShiftRepository _shifts;
    private readonly IStoreSettingsRepository _settings;
    private readonly IItemRepository _items;
    private readonly ITransactionRepository _transactions;
    private readonly IReceiptNumberGenerator _receiptGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public RecordEWalletTransactionCommandHandler(
        IShiftRepository shifts,
        IStoreSettingsRepository settings,
        IItemRepository items,
        ITransactionRepository transactions,
        IReceiptNumberGenerator receiptGenerator,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _shifts = shifts;
        _settings = settings;
        _items = items;
        _transactions = transactions;
        _receiptGenerator = receiptGenerator;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(
        RecordEWalletTransactionCommand request, CancellationToken ct)
    {
        var shift = await _shifts.GetOpenAsync(ct)
            ?? throw new DomainException(
                "No open shift — e-wallet transactions belong to an open shift.");

        var settings = await _settings.GetAsync(ct);
        if (settings?.TrackEWalletFloat != true)
            throw new DomainException(
                "E-wallet tracking is off — turn it on in web admin Settings.");

        var isCashIn = request.Direction == EWalletDirection.CashIn;

        var transaction = new EWalletTransaction
        {
            ShiftId = shift.Id,
            Direction = request.Direction,
            Principal = request.Principal,
            WalletDelta = isCashIn ? -request.Principal : request.Principal,
            DrawerDelta = isCashIn ? request.Principal : -request.Principal,
            CreatedBy = _currentUser.Id
        };

        if (request.Fee > 0m)
        {
            var feeItemId = settings.EWalletFeeItemId
                ?? throw new DomainException(
                    "No e-wallet fee item is set — choose one in web admin Settings.");

            var feeItem = await _items.GetByIdAsync(feeItemId, ct)
                ?? throw new DomainException(
                    "No e-wallet fee item is set — choose one in web admin Settings.");

            if (feeItem.SellingPrice != 1m)
                throw new DomainException(
                    "The e-wallet fee item must be priced at ₱1.00 — fix it in web admin Items.");

            var quantity = (int)decimal.Truncate(request.Fee);

            var feeSale = new Transaction
            {
                ReceiptNumber = await _receiptGenerator.GenerateAsync(ct),
                Subtotal = request.Fee,
                DiscountAmount = 0m,
                Total = request.Fee,
                PaymentType = PaymentType.Cash,
                AmountTendered = request.Fee,
                Change = 0m,
                CreatedBy = _currentUser.Id,
                ShiftId = shift.Id,
                Items =
                [
                    new TransactionItem
                    {
                        ItemId = feeItem.Id,
                        ItemName = feeItem.Name,
                        UnitPrice = feeItem.SellingPrice,
                        CostPrice = feeItem.CostPrice,
                        Quantity = quantity,
                        Discount = 0m,
                        Total = request.Fee
                    }
                ]
            };

            feeSale.AddDomainEvent(new SaleCompletedEvent(
                feeSale.Id, [(feeItem.Id, quantity)], _currentUser.Id));

            await _transactions.AddAsync(feeSale, ct);
            transaction.FeeTransaction = feeSale;
        }

        await _shifts.AddEWalletTransactionAsync(transaction, ct);

        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await _unitOfWork.SaveChangesAsync(ct);
                break;
            }
            catch (ReceiptNumberCollisionException) when (
                attempt < 2 && transaction.FeeTransaction is { } sale)
            {
                sale.ReceiptNumber = await _receiptGenerator.GenerateAsync(ct);
            }
        }

        return transaction.Id;
    }
}
