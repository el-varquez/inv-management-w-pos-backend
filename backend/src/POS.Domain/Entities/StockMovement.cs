using POS.Domain.Common;
using POS.Domain.Enums;

namespace POS.Domain.Entities;

public class StockMovement : BaseEntity
{
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = null!;
    
    public StockMovementType Type { get; set; }
    public int Quantity { get; set; }
    public decimal? CostPerUnit { get; set; }
    public string? SupplierName { get; set; }
    public AdjustmentReason? Reason { get; set; }
    public string? Notes { get; set; }
    public Guid CreatedBy { get; set; }
}