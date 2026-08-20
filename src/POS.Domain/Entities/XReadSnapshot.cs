namespace POS.Domain.Entities;

public class XReadSnapshot
{
    public decimal NetSales { get; set; }
    public int TransactionCount { get; set; }

    public decimal CashSales { get; set; }
    public decimal GcashSales { get; set; }
    public decimal MayaSales { get; set; }

    public decimal Refunds { get; set; }
    public int RefundCount { get; set; }

    public decimal DrawerMovementsNet { get; set; }

    public decimal ExpectedCash { get; set; }
    public decimal CountedCash { get; set; }
    public decimal CashVariance { get; set; }

    public decimal? CountedCashOriginal { get; set; }
    public DateTime? CorrectedAt { get; set; }
    public Guid? CorrectedBy { get; set; }
    public string? CorrectionReason { get; set; }

    public decimal? ExpectedEWalletBalance { get; set; }
    public decimal? CountedEWalletBalance { get; set; }
    public decimal? EWalletVariance { get; set; }
}
