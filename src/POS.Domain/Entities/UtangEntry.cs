using POS.Domain.Common;
using POS.Domain.Enums;

namespace POS.Domain.Entities;

public class UtangEntry : BaseEntity
{
    public Guid SukiId { get; set; }
    public Suki Suki { get; set; } = null!;

    public UtangEntryType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal Markup { get; set; }

    public Guid? TransactionId { get; set; }
    public Transaction? Transaction { get; set; }

    public Guid ShiftId { get; set; }
    public string? Note { get; set; }

    public bool IsVoided { get; set; }
    public DateTime? VoidedAt { get; set; }
    public Guid? VoidedBy { get; set; }

    public decimal? EditedFrom { get; set; }
    public Guid CreatedBy { get; set; }
}
