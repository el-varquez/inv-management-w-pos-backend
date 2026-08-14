using POS.Domain.Common;

namespace POS.Domain.Entities;

public class User : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PasswordHash { get; set; }
    public string Role { get; set; } = "Cashier";
    public bool IsActive { get; set; } = true;
}