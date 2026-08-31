using POS.Domain.Common;
using POS.Domain.Enums;

namespace POS.Domain.Entities;

public class PaymentMethod : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public PaymentMethodType Type { get; set; }
    public bool RequiresReference { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsSystem { get; set; }
}
