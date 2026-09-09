using POS.Domain.Common;

namespace POS.Domain.Entities;

public class Invoice : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid SukiId { get; set; }
    public Suki Suki { get; set; } = null!;
    public Guid ShiftId { get; set; }
    public Shift Shift { get; set; } = null!;
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal MarkupTotal { get; set; }
    public decimal Total { get; set; }
    public bool IsVoided { get; set; }
    public DateTime? VoidedAt { get; set; }
    public Guid? VoidedBy { get; set; }
    public Guid CreatedBy { get; set; }

    public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
}
