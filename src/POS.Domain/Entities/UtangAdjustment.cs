using POS.Domain.Common;

namespace POS.Domain.Entities;

public class UtangAdjustment : BaseEntity
{
    public Guid SukiId { get; set; }
    public Suki Suki { get; set; } = null!;

    public decimal Amount { get; set; }
    public string Note { get; set; } = string.Empty;

    public bool IsVoided { get; set; }
    public DateTime? VoidedAt { get; set; }
    public Guid? VoidedBy { get; set; }

    public Guid CreatedBy { get; set; }
}
