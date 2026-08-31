namespace POS.Domain.Entities;

public class XReadSnapshot
{
    public decimal NetSales { get; set; }
    public int TransactionCount { get; set; }

    public int UtangChargedCount { get; set; }
    public decimal UtangCharged { get; set; }
    public decimal UtangMarkup { get; set; }
    public decimal UtangCollections { get; set; }

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
}
