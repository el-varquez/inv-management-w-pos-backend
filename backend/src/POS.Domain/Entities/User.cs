using POS.Domain.Common;

namespace POS.Domain.Entities;

public class User : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "Cashier";
    public bool IsActive { get; set; } = true;
    /// <summary>Owning tenant. Null only for the platform SuperAdmin.</summary>
    public Guid? TenantId { get; set; }
}