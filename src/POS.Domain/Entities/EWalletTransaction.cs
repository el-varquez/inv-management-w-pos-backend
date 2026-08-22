using POS.Domain.Common;
using POS.Domain.Enums;

namespace POS.Domain.Entities;

public class EWalletTransaction : BaseEntity
{
    public Guid ShiftId { get; set; }
    public Shift Shift { get; set; } = null!;

    public EWalletDirection Direction { get; set; }
    public decimal Principal { get; set; }
    public decimal WalletDelta { get; set; }
    public decimal DrawerDelta { get; set; }

    public string? Reason { get; set; }

    public Guid? FeeTransactionId { get; set; }
    public Transaction? FeeTransaction { get; set; }

    public bool IsVoided { get; set; }
    public DateTime? VoidedAt { get; set; }
    public Guid? VoidedBy { get; set; }

    public Guid CreatedBy { get; set; }
}
