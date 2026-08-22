using MediatR;
using POS.Application.Common.Interfaces;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Domain.Events;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Shifts.Commands.VoidEWalletTransaction;

public class VoidEWalletTransactionCommandHandler
    : IRequestHandler<VoidEWalletTransactionCommand>
{
    private readonly IShiftRepository _shifts;
    private readonly ITransactionRepository _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public VoidEWalletTransactionCommandHandler(
        IShiftRepository shifts,
        ITransactionRepository transactions,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _shifts = shifts;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(VoidEWalletTransactionCommand request, CancellationToken ct)
    {
        var transaction = await _shifts.GetEWalletTransactionByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("E-wallet transaction", request.Id);

        if (transaction.IsVoided)
            throw new DomainException("This e-wallet transaction is already voided.");

        var shift = await _shifts.GetByIdAsync(transaction.ShiftId, ct);
        if (shift is null || shift.Status != ShiftStatus.Open)
            throw new DomainException(
                "The shift is closed — its e-wallet transactions can no longer be changed.");

        transaction.IsVoided = true;
        transaction.VoidedAt = DateTime.UtcNow;
        transaction.VoidedBy = _currentUser.Id;

        if (transaction.FeeTransactionId is { } feeId)
        {
            var fee = await _transactions.GetByIdAsync(feeId, ct);
            if (fee is not null && !fee.IsRefunded)
            {
                fee.IsRefunded = true;
                fee.UpdatedAt = DateTime.UtcNow;
                await _transactions.UpdateAsync(fee, ct);

                var mirror = new Transaction
                {
                    ReceiptNumber = fee.ReceiptNumber + "-V",
                    Subtotal = -fee.Subtotal,
                    DiscountAmount = -fee.DiscountAmount,
                    Total = -fee.Total,
                    PaymentType = fee.PaymentType,
                    AmountTendered = 0m,
                    Change = 0m,
                    RefundedFromId = fee.Id,
                    CreatedBy = _currentUser.Id,
                    ShiftId = shift.Id,
                    Items = fee.Items.Select(i => new TransactionItem
                    {
                        ItemId = i.ItemId,
                        ItemName = i.ItemName,
                        UnitPrice = i.UnitPrice,
                        CostPrice = i.CostPrice,
                        Quantity = -i.Quantity,
                        Discount = -i.Discount,
                        Total = -i.Total
                    }).ToList()
                };

                mirror.AddDomainEvent(new SaleRefundedEvent(
                    mirror.Id,
                    fee.Items.Select(i => (i.ItemId, i.Quantity)).ToList(),
                    _currentUser.Id));

                await _transactions.AddAsync(mirror, ct);
            }
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
