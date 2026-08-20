using POS.Domain.Common;
using POS.Domain.Enums;

namespace POS.Domain.Entities;

public class Shift : BaseEntity
{
    public int Number { get; set; }
    public ShiftStatus Status { get; set; } = ShiftStatus.Open;

    public decimal StartingCash { get; set; }
    public decimal? StartingCashOriginal { get; set; }
    public DateTime? StartingCashCorrectedAt { get; set; }
    public Guid? StartingCashCorrectedBy { get; set; }
    public string? StartingCashCorrectionReason { get; set; }

    public decimal? StartingEWalletBalance { get; set; }

    public DateTime OpenedAt { get; set; }
    public Guid OpenedBy { get; set; }
    public DateTime? ClosedAt { get; set; }
    public Guid? ClosedBy { get; set; }

    public Guid BusinessDayId { get; set; }
    public BusinessDay BusinessDay { get; set; } = null!;

    public XReadSnapshot? Snapshot { get; set; }

    public ICollection<CashDrawerMovement> DrawerMovements { get; set; }
        = new List<CashDrawerMovement>();
}
