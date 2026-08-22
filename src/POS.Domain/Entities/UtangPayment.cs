using POS.Domain.Common;

namespace POS.Domain.Entities;

public class UtangPayment : BaseEntity
{
    public Guid SukiId { get; set; }
    public Suki Suki { get; set; } = null!;

    public decimal Amount { get; set; }

    public Guid? TransactionId { get; set; }
    public Transaction? Transaction { get; set; }

    public Guid ShiftId { get; set; }

    public bool IsVoided { get; set; }
    public DateTime? VoidedAt { get; set; }
    public Guid? VoidedBy { get; set; }

    public decimal? EditedFrom { get; set; }
    public Guid CreatedBy { get; set; }
}
