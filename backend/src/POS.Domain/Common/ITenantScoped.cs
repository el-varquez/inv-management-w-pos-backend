namespace POS.Domain.Common;

/// <summary>
/// Marks an entity as owned by a single tenant. Every implementer is
/// automatically scoped by the EF global query filter (reads) and
/// auto-stamped with the current tenant on insert (writes).
/// </summary>
public interface ITenantScoped
{
    Guid TenantId { get; set; }
}
