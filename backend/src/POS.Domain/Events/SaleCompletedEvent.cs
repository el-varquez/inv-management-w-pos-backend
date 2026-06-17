using POS.Domain.Common;

namespace POS.Domain.Events;

public class SaleCompletedEvent : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid TransactionId { get; }
    public IReadOnlyList<(Guid ItemId, int Quantity)> SoldItems { get; }
    public Guid CreatedBy { get; }

    public SaleCompletedEvent(
        Guid transactionId,
        IReadOnlyList<(Guid ItemId, int Quantity)> soldItems,
        Guid createdBy)
    {
        TransactionId = transactionId;
        SoldItems = soldItems;
        CreatedBy = createdBy;
    }
}