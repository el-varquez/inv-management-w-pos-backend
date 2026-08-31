using POS.Domain.Common;

namespace POS.Domain.Entities;

public class DayMethodSales : BaseEntity
{
    public Guid BusinessDayId { get; set; }
    public BusinessDay? BusinessDay { get; set; }
    public Guid PaymentMethodId { get; set; }
    public string MethodName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
