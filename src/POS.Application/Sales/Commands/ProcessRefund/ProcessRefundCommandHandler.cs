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
    private readonly ISaleRepository _sales;
    private readonly IReceiptNumberGenerator _receiptGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IShiftRepository _shifts;

    public ProcessRefundCommandHandler(
        ISaleRepository saleRepository,
        IReceiptNumberGenerator receiptGenerator,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IShiftRepository shifts)
    {
        _sales = saleRepository;
        _receiptGenerator = receiptGenerator;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _shifts = shifts;
    }

    public async Task<RefundResult> Handle(
        ProcessRefundCommand request, CancellationToken ct)
    {
        var shift = await _shifts.GetOpenAsync(ct)
            ?? throw new DomainException(
                "No open shift — voids land in the current shift. Declare starting cash first.");

        var original = await _sales.GetByIdAsync(request.SaleId, ct)
            ?? throw new NotFoundException("Sale", request.SaleId);

        if (original.IsRefunded)
            throw new DomainException("This sale has already been refunded.");

        original.IsRefunded = true;
        original.UpdatedAt = DateTime.UtcNow;
        await _sales.UpdateAsync(original, ct);

        var receiptNumber = await _receiptGenerator.GenerateAsync(ct);

        var refund = new Sale
        {
            ReceiptNumber = receiptNumber,
            Subtotal = -original.Subtotal,
            DiscountAmount = -original.DiscountAmount,
            Total = -original.Total,
            PaymentMethodId = original.PaymentMethodId,
            AmountTendered = 0,
            Change = 0,
            IsRefunded = false,
            RefundedFromId = original.Id,
            CreatedBy = _currentUser.Id,
            ShiftId = shift.Id,
            Items = original.Items.Select(i => new SaleItem
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

        await _sales.AddAsync(refund, ct);

        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await _unitOfWork.SaveChangesAsync(ct);
                break;
            }
            catch (ReceiptNumberCollisionException) when (attempt < 2)
            {
                refund.ReceiptNumber = await _receiptGenerator.GenerateAsync(ct);
            }
        }

        return new RefundResult(refund.Id, refund.ReceiptNumber, original.Total);
    }
}
