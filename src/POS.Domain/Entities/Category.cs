using POS.Domain.Common;

namespace POS.Domain.Entities;

public class Category : BaseEntity, ITenantScoped
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<Item> Items { get; set; }= new List<Item>();
}