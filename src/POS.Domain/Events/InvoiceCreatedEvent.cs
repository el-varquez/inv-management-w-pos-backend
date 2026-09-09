using POS.Domain.Common;

namespace POS.Domain.Events;

public class InvoiceCreatedEvent : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid InvoiceId { get; }
    public IReadOnlyList<(Guid ItemId, int Quantity)> SoldItems { get; }
    public Guid CreatedBy { get; }

    public InvoiceCreatedEvent(
        Guid invoiceId,
        IReadOnlyList<(Guid ItemId, int Quantity)> soldItems,
        Guid createdBy)
    {
        InvoiceId = invoiceId;
        SoldItems = soldItems;
        CreatedBy = createdBy;
    }
}
