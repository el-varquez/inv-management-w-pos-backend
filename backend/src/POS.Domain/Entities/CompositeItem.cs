using POS.Domain.Common;

namespace POS.Domain.Entities;

public class CompositeItem : BaseEntity, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid ParentItemId { get; set; }
    public Item ParentItem { get; set; } = null!;

    public Guid ComponentItemId { get; set; }
    public Item ComponentItem { get; set; } = null!;

    public decimal Quantity { get; set; }
}