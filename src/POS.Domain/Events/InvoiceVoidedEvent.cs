using POS.Domain.Common;

namespace POS.Domain.Events;

public class InvoiceVoidedEvent : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid InvoiceId { get; }
    public IReadOnlyList<(Guid ItemId, int Quantity)> RestockedItems { get; }
    public Guid CreatedBy { get; }

    public InvoiceVoidedEvent(
        Guid invoiceId,
        IReadOnlyList<(Guid ItemId, int Quantity)> restockedItems,
        Guid createdBy)
    {
        InvoiceId = invoiceId;
        RestockedItems = restockedItems;
        CreatedBy = createdBy;
    }
}
