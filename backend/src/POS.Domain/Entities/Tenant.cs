using POS.Domain.Common;

namespace POS.Domain.Entities;

public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Max active cashiers allowed for this tenant (first plan limit).</summary>
    public int CashierCap { get; set; } = 5;
}
