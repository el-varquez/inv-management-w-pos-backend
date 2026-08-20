using POS.Domain.Common;
using POS.Domain.Enums;

namespace POS.Domain.Entities;

public class BusinessDay : BaseEntity
{
    public int Number { get; set; }
    public DayStatus Status { get; set; } = DayStatus.Open;

    public DateTime OpenedAt { get; set; }
    public Guid OpenedBy { get; set; }
    public DateTime? ClosedAt { get; set; }
    public Guid? ClosedBy { get; set; }
    public bool ClosedLate { get; set; }

    public ZReadSnapshot? Snapshot { get; set; }

    public ICollection<Shift> Shifts { get; set; } = new List<Shift>();
}
