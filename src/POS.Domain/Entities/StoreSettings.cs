using POS.Domain.Common;

namespace POS.Domain.Entities;

public class StoreSettings : BaseEntity
{
    public string StoreName { get; set; } = "My Store";
    public string Address { get; set; } = string.Empty;
    public string ReceiptFooter { get; set; } = string.Empty;
    public bool AcceptUtang { get; set; } = true;
    public decimal DefaultUtangMarkup { get; set; }
    public bool TrackGcashWallet { get; set; } = false;
    public Guid? GcashFeeItemId { get; set; }
}
