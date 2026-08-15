using POS.Domain.Common;

namespace POS.Domain.Entities;

public class CashDrawerMovement : BaseEntity
{
    public Guid ShiftId { get; set; }
    public Shift Shift { get; set; } = null!;

    public decimal Amount { get; set; }
    public string Note { get; set; } = string.Empty;

    public bool IsVoided { get; set; }
    public DateTime? VoidedAt { get; set; }
    public Guid? VoidedBy { get; set; }

    public Guid CreatedBy { get; set; }
}
