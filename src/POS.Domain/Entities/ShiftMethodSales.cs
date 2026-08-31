using POS.Domain.Common;

namespace POS.Domain.Entities;

public class ShiftMethodSales : BaseEntity
{
    public Guid ShiftId { get; set; }
    public Shift? Shift { get; set; }
    public Guid PaymentMethodId { get; set; }
    public string MethodName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
