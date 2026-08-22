using POS.Domain.Common;

namespace POS.Domain.Entities;

public class UtangCharge : BaseEntity
{
    public Guid SukiId { get; set; }
    public Suki Suki { get; set; } = null!;

    public decimal Amount { get; set; }
    public decimal Markup { get; set; }

    public Guid TransactionId { get; set; }
    public Transaction? Transaction { get; set; }

    public Guid ShiftId { get; set; }

    public bool IsVoided { get; set; }
    public DateTime? VoidedAt { get; set; }
    public Guid? VoidedBy { get; set; }

    public Guid CreatedBy { get; set; }
}
