using MediatR;
using POS.Application.Common.Interfaces;
using POS.Domain.Entities;
using POS.Domain.Events;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Sales.Commands.ProcessRefund;

public class ProcessRefundCommandHandler
    : IRequestHandler<ProcessRefundCommand, RefundResult>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IReceiptNumberGenerator _receiptGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public ProcessRefundCommandHandler(
        ITransactionRepository transactionRepository,
        IReceiptNumberGenerator receiptGenerator,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _transactionRepository = transactionRepository;
        _receiptGenerator = receiptGenerator;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<RefundResult> Handle(
        ProcessRefundCommand request, CancellationToken ct)
    {
        var original = await _transactionRepository.GetByIdAsync(request.TransactionId, ct)
            ?? throw new NotFoundException("Transaction", request.TransactionId);

        if (original.IsRefunded)
            throw new DomainException("This transaction has already been refunded.");

        original.IsRefunded = true;
        original.UpdatedAt = DateTime.UtcNow;
        await _transactionRepository.UpdateAsync(original, ct);

        var receiptNumber = await _receiptGenerator.GenerateAsync(ct);

        var refund = new Transaction
        {
            ReceiptNumber = receiptNumber,
            Subtotal = -original.Subtotal,
            DiscountAmount = -original.DiscountAmount,
            Total = -original.Total,
            PaymentType = original.PaymentType,
            AmountTendered = 0,
            Change = 0,
            IsRefunded = false,
            RefundedFromId = original.Id,
            CreatedBy = _currentUser.Id,
            Items = original.Items.Select(i => new TransactionItem
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

        var refundedItems = original.Items
            .Select(i => (i.ItemId, i.Quantity))
            .ToList();
        refund.AddDomainEvent(
            new SaleRefundedEvent(refund.Id, refundedItems, _currentUser.Id));

        await _transactionRepository.AddAsync(refund, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new RefundResult(refund.Id, receiptNumber, original.Total);
    }
}