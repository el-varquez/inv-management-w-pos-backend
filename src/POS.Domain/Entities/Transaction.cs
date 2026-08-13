using POS.Domain.Common;
using POS.Domain.Enums;

namespace POS.Domain.Entities;

public class Transaction : BaseEntity
{
    public string ReceiptNumber { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }
    public PaymentType PaymentType { get; set; }
    public decimal AmountTendered { get; set; }
    public decimal Change { get; set; }
    public bool IsRefunded { get; set; }
    public Guid? RefundedFromId { get; set; }
    public Guid CreatedBy { get; set; }

    public ICollection<TransactionItem> Items { get; set; } = new List<TransactionItem>();
}