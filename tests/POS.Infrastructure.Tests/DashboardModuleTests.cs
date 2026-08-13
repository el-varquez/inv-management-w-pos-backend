using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Application.Dashboard.Queries.GetDashboardSummary;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;
using Xunit;

namespace POS.Infrastructure.Tests;

public class DashboardModuleTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _ctx;
    private readonly TransactionRepository _transactions;
    private readonly ItemRepository _items;

    // 2 AM local today / yesterday, as the UTC instants the DB stores.
    private static readonly DateTime TodayUtc =
        DateTime.Today.AddHours(2).ToUniversalTime();
    private static readonly DateTime YesterdayUtc = TodayUtc.AddDays(-1);

    public DashboardModuleTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _ctx = new AppDbContext(options);
        _ctx.Database.EnsureCreated();

        _transactions = new TransactionRepository(_ctx);
        _items = new ItemRepository(_ctx);
    }

    private async Task<Transaction> SeedSale(
        decimal total, DateTime createdAtUtc,
        PaymentType payment = PaymentType.Cash, Guid? refundOf = null)
    {
        var t = new Transaction
        {
            ReceiptNumber = Guid.NewGuid().ToString("N")[..10],
            Subtotal = Math.Abs(total),
            Total = total,
            PaymentType = payment,
            RefundedFromId = refundOf,
            CreatedBy = Guid.NewGuid(),
            CreatedAt = createdAtUtc,
        };
        _ctx.Transactions.Add(t);
        await _ctx.SaveChangesAsync();
        return t;
    }

    private async Task SeedItem(string name, int stock, int threshold,
        bool isActive = true, bool isComposite = false)
    {
        var cat = await _ctx.Categories.FirstOrDefaultAsync();
        if (cat == null)
        {
            cat = new Category { Name = "Sari-sari" };
            _ctx.Categories.Add(cat);
            await _ctx.SaveChangesAsync();
        }
        _ctx.Items.Add(new Item
        {
            Name = name,
            CostPrice = 5,
            SellingPrice = 10,
            Stock = stock,
            LowStockThreshold = threshold,
            IsActive = isActive,
            IsComposite = isComposite,
            CategoryId = cat.Id,
        });
        await _ctx.SaveChangesAsync();
    }

    private GetDashboardSummaryQueryHandler SummaryHandler()
        => new(_transactions, _items);

    [Fact]
    public async Task Today_kpis_net_out_refunds_and_average()
    {
        var sale = await SeedSale(300m, TodayUtc);
        await SeedSale(200m, TodayUtc.AddMinutes(5));
        await SeedSale(-100m, TodayUtc.AddMinutes(10), refundOf: sale.Id);

        var result = await SummaryHandler()
            .Handle(new GetDashboardSummaryQuery(), CancellationToken.None);

        Assert.Equal(400m, result.Today.PaidSales);       // 300 + 200 − 100
        Assert.Equal(2, result.Today.TransactionCount);   // refunds don't count
        Assert.Equal(200m, result.Today.AverageSale);
    }

    [Fact]
    public async Task Delta_is_null_without_yesterday_sales_and_computed_with_them()
    {
        await SeedSale(250m, TodayUtc);
        var noYesterday = await SummaryHandler()
            .Handle(new GetDashboardSummaryQuery(), CancellationToken.None);
        Assert.Null(noYesterday.Today.DeltaPercent);

        await SeedSale(200m, YesterdayUtc);
        var withYesterday = await SummaryHandler()
            .Handle(new GetDashboardSummaryQuery(), CancellationToken.None);
        Assert.Equal(200m, withYesterday.Today.YesterdayPaidSales);
        Assert.Equal(25.0m, withYesterday.Today.DeltaPercent); // (250−200)/200
    }

    [Fact]
    public async Task Stock_health_counts_in_low_out_and_skips_inactive_and_composites()
    {
        await SeedItem("Plenty", stock: 10, threshold: 5);        // in
        await SeedItem("Scarce", stock: 3, threshold: 5);         // low
        await SeedItem("Gone", stock: 0, threshold: 5);           // out
        await SeedItem("Retired", stock: 0, threshold: 5, isActive: false);
        await SeedItem("Bundle", stock: 0, threshold: 5, isComposite: true);

        var result = await SummaryHandler()
            .Handle(new GetDashboardSummaryQuery(), CancellationToken.None);

        Assert.Equal(3, result.StockHealth.TotalItems);
        Assert.Equal(1, result.StockHealth.InStock);
        Assert.Equal(1, result.StockHealth.LowStock);
        Assert.Equal(1, result.StockHealth.OutOfStock);
    }

    [Fact]
    public async Task Running_out_orders_lowest_stock_then_biggest_deficit_capped_at_5()
    {
        await SeedItem("OutBig", stock: 0, threshold: 24);   // deficit 24 → first
        await SeedItem("OutSmall", stock: 0, threshold: 12); // deficit 12 → second
        await SeedItem("Low1", stock: 2, threshold: 20);
        await SeedItem("Low2", stock: 3, threshold: 20);
        await SeedItem("Low3", stock: 4, threshold: 20);
        await SeedItem("Low4", stock: 5, threshold: 20);     // 6th at/below → cut
        await SeedItem("Fine", stock: 50, threshold: 5);

        var result = await SummaryHandler()
            .Handle(new GetDashboardSummaryQuery(), CancellationToken.None);

        Assert.Equal(5, result.RunningOut.Count);
        Assert.Equal("OutBig", result.RunningOut[0].Name);
        Assert.Equal("OutSmall", result.RunningOut[1].Name);
        Assert.DoesNotContain(result.RunningOut, r => r.Name == "Fine");
        Assert.DoesNotContain(result.RunningOut, r => r.Name == "Low4");
    }

    [Fact]
    public async Task Payments_split_by_method_always_has_all_three_rows()
    {
        await SeedSale(100m, TodayUtc, PaymentType.Cash);
        await SeedSale(50m, TodayUtc, PaymentType.Gcash);
        await SeedSale(999m, YesterdayUtc, PaymentType.Maya); // not today → excluded

        var result = await SummaryHandler()
            .Handle(new GetDashboardSummaryQuery(), CancellationToken.None);

        Assert.Equal(3, result.PaymentsToday.Count);
        Assert.Equal(100m, result.PaymentsToday.Single(p => p.Method == "Cash").Amount);
        Assert.Equal(50m, result.PaymentsToday.Single(p => p.Method == "GCash").Amount);
        Assert.Equal(0m, result.PaymentsToday.Single(p => p.Method == "Maya").Amount);
        Assert.Equal(1, result.PaymentsToday.Single(p => p.Method == "Cash").TransactionCount);
    }

    [Fact]
    public async Task Utang_snapshot_is_the_zero_stub()
    {
        var result = await SummaryHandler()
            .Handle(new GetDashboardSummaryQuery(), CancellationToken.None);

        Assert.Equal(0m, result.Utang.TotalOutstanding);
        Assert.Equal(0, result.Utang.SukiCount);
        Assert.Equal(0m, result.Utang.CollectedThisWeek);
        Assert.Empty(result.Utang.Top);
    }

    private POS.Application.Dashboard.Queries.GetSalesTrend.GetSalesTrendQueryHandler
        TrendHandler() => new(_transactions);

    [Fact]
    public async Task Week_trend_returns_7_zero_filled_buckets_with_sales_in_the_right_day()
    {
        await SeedSale(500m, YesterdayUtc);
        await SeedSale(300m, TodayUtc);

        var result = await TrendHandler().Handle(
            new POS.Application.Dashboard.Queries.GetSalesTrend.GetSalesTrendQuery("week"),
            CancellationToken.None);

        Assert.Equal(7, result.Buckets.Count);
        Assert.Equal(DateTime.Today.AddDays(-6), result.Buckets[0].BucketStart);
        Assert.Equal(500m, result.Buckets[5].PaidSales);  // yesterday
        Assert.Equal(300m, result.Buckets[6].PaidSales);  // today
        Assert.Equal(800m, result.TotalPaidSales);
        Assert.All(result.Buckets, b => Assert.Equal(0m, b.UtangCharged));
        Assert.Equal(0m, result.TotalUtangCharged);
    }

    [Fact]
    public async Task Refunds_net_inside_their_own_bucket()
    {
        var sale = await SeedSale(300m, TodayUtc);
        await SeedSale(-100m, TodayUtc.AddMinutes(10), refundOf: sale.Id);

        var result = await TrendHandler().Handle(
            new POS.Application.Dashboard.Queries.GetSalesTrend.GetSalesTrendQuery("week"),
            CancellationToken.None);

        Assert.Equal(200m, result.Buckets[6].PaidSales);
    }

    [Fact]
    public async Task Day_trend_returns_24_hourly_buckets()
    {
        await SeedSale(150m, TodayUtc);        // 2 AM local
        await SeedSale(50m, TodayUtc.AddMinutes(30));

        var result = await TrendHandler().Handle(
            new POS.Application.Dashboard.Queries.GetSalesTrend.GetSalesTrendQuery("day"),
            CancellationToken.None);

        Assert.Equal(24, result.Buckets.Count);
        Assert.Equal(200m, result.Buckets[2].PaidSales); // the 2 AM bucket
        Assert.Equal(200m, result.TotalPaidSales);
    }

    [Fact]
    public async Task Year_trend_returns_12_monthly_buckets_ending_this_month()
    {
        await SeedSale(1000m, TodayUtc);

        var result = await TrendHandler().Handle(
            new POS.Application.Dashboard.Queries.GetSalesTrend.GetSalesTrendQuery("year"),
            CancellationToken.None);

        Assert.Equal(12, result.Buckets.Count);
        var thisMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        Assert.Equal(thisMonth, result.Buckets[11].BucketStart);
        Assert.Equal(1000m, result.Buckets[11].PaidSales);
    }

    [Fact]
    public void Trend_validator_rejects_unknown_period_and_accepts_any_case()
    {
        var validator = new POS.Application.Dashboard.Queries.GetSalesTrend
            .GetSalesTrendQueryValidator();

        Assert.False(validator.Validate(
            new POS.Application.Dashboard.Queries.GetSalesTrend.GetSalesTrendQuery("decade")).IsValid);
        Assert.True(validator.Validate(
            new POS.Application.Dashboard.Queries.GetSalesTrend.GetSalesTrendQuery("Week")).IsValid);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }
}
