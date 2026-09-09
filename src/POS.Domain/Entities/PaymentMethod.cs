using POS.Domain.Common;

namespace POS.Domain.Entities;

public class PaymentMethod : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool RequiresReference { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsSystem { get; set; }
}
