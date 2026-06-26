using System.ComponentModel;
using POS.Domain.Common;

namespace POS.Domain.Entities;

public class Item : BaseEntity, ITenantScoped
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Sku { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public int Stock { get ; set; }
    public int LowStockThreshold { get; set; } = 5;
    public bool IsActive { get; set; } = true;

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    public ICollection<TransactionItem> TransactionItems { get; set; } = new List<TransactionItem>();
    public ICollection<CompositeItem> Components { get; set; } = new List<CompositeItem>(); 
    public ICollection<CompositeItem> UsedInItems { get; set; } = new List<CompositeItem>();

    public bool IsComposite { get; set; } = false;
}