using POS.Domain.Common;
using POS.Domain.Enums;

namespace POS.Domain.Entities;

public class InventoryCount : BaseEntity
{
    public string Reference { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public InventoryCountStatus Status { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid CreatedBy { get; set; }

    public ICollection<InventoryCountLine> Lines { get; set; } = new List<InventoryCountLine>();
}