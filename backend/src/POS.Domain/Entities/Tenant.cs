using POS.Domain.Common;

namespace POS.Domain.Entities;

public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int CashierCap { get; set; } = 5;
}
