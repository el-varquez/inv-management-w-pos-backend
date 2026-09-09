using POS.Domain.Common;

namespace POS.Domain.Entities;

public class SaleItem : BaseEntity
{
    public Guid SaleId { get; set; }
    public Sale Sale { get; set; } = null!;

    public Guid ItemId { get; set; }
    public Item Item { get; set; } = null!;

    public string ItemName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal CostPrice { get; set; }
    public int Quantity { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
}
