using POS.Domain.Common;

namespace POS.Domain.Entities;

public class InventoryCountLine : BaseEntity
{
    public Guid InventoryCountId { get; set; }
    public InventoryCount InventoryCount { get; set; } = null!;

    public Guid ItemId { get; set; }
    public Item Item { get; set; } = null!;

    public int ExpectedQty { get; set; }
    public int ActualQty { get; set; }
    public int Variance => ActualQty - ExpectedQty;
}