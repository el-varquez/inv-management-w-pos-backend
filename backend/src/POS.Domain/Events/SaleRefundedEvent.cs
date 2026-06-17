using POS.Domain.Common;

namespace POS.Domain.Events;

public class SaleRefundedEvent : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid TransactionId { get; }
    public IReadOnlyList<(Guid ItemId, int Quantity)> RefundedItems { get; }
    public Guid CreatedBy { get; }

    public SaleRefundedEvent(
        Guid transactionId,
        IReadOnlyList<(Guid ItemId, int Quantity)> refundedItems,
        Guid createdBy)
    {
        TransactionId = transactionId;
        RefundedItems = refundedItems;
        CreatedBy = createdBy;
    }
}