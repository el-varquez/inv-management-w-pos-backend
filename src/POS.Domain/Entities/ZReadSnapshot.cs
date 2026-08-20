namespace POS.Domain.Entities;

public class ZReadSnapshot
{
    public decimal NetSales { get; set; }
    public int TransactionCount { get; set; }

    public decimal CashSales { get; set; }
    public decimal GcashSales { get; set; }
    public decimal MayaSales { get; set; }

    public decimal DrawerMovementsNet { get; set; }

    public decimal CountedCash { get; set; }
    public decimal CashVariance { get; set; }

    public decimal? CountedEWalletBalance { get; set; }
    public decimal? EWalletVariance { get; set; }

    public int ShiftCount { get; set; }
}
