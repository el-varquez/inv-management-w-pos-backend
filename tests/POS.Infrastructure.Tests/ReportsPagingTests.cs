using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Application.Reports.Queries.GetBestSellers;
using POS.Application.Reports.Queries.GetExpenseReport;
using POS.Application.Reports.Queries.GetProfitReport;
using POS.Application.Reports.Queries.GetSalesReport;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;
using POS.Infrastructure.Tests.Fakes;
using Xunit;

namespace POS.Infrastructure.Tests;

/// <summary>
/// Report paging and SQL totals. Sales, refunds, invoices and movements are
/// inserted directly so each fact controls dates and amounts exactly.
/// </summary>
public class ReportsPagingTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _ctx;
    private readonly ItemRepository _items;
    private readonly SaleRepository _sales;
    private readonly InvoiceRepository _invoices;
    private readonly StockMovementRepository _movements;
    private readonly UnitOfWork _uow;
    private readonly FakeCurrentUser _user = new();
    private readonly Guid _categoryId = Guid.NewGuid();
    private readonly Shift _shift;
    private int _seq;

    private static readonly DateTime Day1 = new(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Day2 = new(2026, 10, 2, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Day3 = new(2026, 10, 3, 9, 0, 0, DateTimeKind.Utc);

    public ReportsPagingTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _ctx = new AppDbContext(options);
        _ctx.Database.EnsureCreated();
        PaymentMethodSeeder.Seed(_ctx);

        _items = new ItemRepository(_ctx);
        _sales = new SaleRepository(_ctx);
        _invoices = new InvoiceRepository(_ctx);
        _movements = new StockMovementRepository(_ctx);
        _uow = new UnitOfWork(_ctx);

        _ctx.Categories.Add(new Category { Id = _categoryId, Name = "General" });
        _shift = new Shift
        {
            Number = 1,
            Status = ShiftStatus.Open,
            StartingCash = 1000m,
            OpenedAt = DateTime.UtcNow,
            OpenedBy = _user.Id,
            BusinessDay = new BusinessDay
            {
                Number = 1,
                Status = DayStatus.Open,
                OpenedAt = DateTime.UtcNow,
                OpenedBy = _user.Id
            }
        };
        _ctx.Shifts.Add(_shift);
        _ctx.SaveChanges();
    }

    // ---------- seeding helpers ----------

    private async Task<Item> SeedItemAsync(string name, decimal cost = 10m, decimal price = 20m)
    {
        var item = new Item
        {
            Name = name,
            ItemCode = $"T{++_seq:D4}",
            CostPrice = cost,
            SellingPrice = price,
            Stock = 100,
            CategoryId = _categoryId
        };
        await _items.AddAsync(item);
        await _uow.SaveChangesAsync();
        return item;
    }

    private record Line(Item Item, int Qty, decimal Total);

    /// <summary>A paid sale (or, with <paramref name="refundedFromId"/>, a refund row).</summary>
    private async Task<Sale> SeedSaleAsync(
        DateTime createdAt,
        decimal subtotal,
        decimal discount,
        decimal total,
        Guid? refundedFromId = null,
        params Line[] lines)
    {
        var sale = new Sale
        {
            ReceiptNumber = $"R{++_seq:D4}",
            Subtotal = subtotal,
            DiscountAmount = discount,
            Total = total,
            PaymentMethodId = PaymentMethodIds.Cash,
            AmountTendered = total,
            Change = 0m,
            RefundedFromId = refundedFromId,
            CreatedBy = _user.Id,
            ShiftId = _shift.Id,
            CreatedAt = createdAt,
            Items = lines.Select(l => new SaleItem
            {
                ItemId = l.Item.Id,
                ItemName = l.Item.Name,
                UnitPrice = l.Item.SellingPrice,
                CostPrice = l.Item.CostPrice,
                Quantity = l.Qty,
                Total = l.Total,
                CreatedAt = createdAt
            }).ToList()
        };
        await _sales.AddAsync(sale);
        await _uow.SaveChangesAsync();
        return sale;
    }

    private async Task SeedThreeDaysOfSalesAsync(Item? item = null)
    {
        item ??= await SeedItemAsync("Rice 1kg");
        await SeedSaleAsync(Day1, 100m, 10m, 90m, lines: new Line(item, 5, 90m));
        await SeedSaleAsync(Day1.AddHours(2), 150m, 0m, 150m, lines: new Line(item, 7, 150m));
        var paid = await SeedSaleAsync(Day2, 200m, 0m, 200m, lines: new Line(item, 10, 200m));
        await SeedSaleAsync(Day2.AddHours(1), 50m, 0m, -50m, paid.Id, new Line(item, -2, -50m));
        await SeedSaleAsync(Day3, 60m, 0m, 60m, lines: new Line(item, 3, 60m));
    }

    private async Task<StockMovement> SeedMovementAsync(
        Item item,
        StockMovementType type,
        int qty,
        decimal? cost,
        AdjustmentReason? reason,
        DateTime createdAt,
        string? supplier = null)
    {
        var movement = new StockMovement
        {
            ItemId = item.Id,
            Type = type,
            Quantity = qty,
            CostPerUnit = cost,
            Reason = reason,
            SupplierName = supplier,
            CreatedBy = _user.Id,
            CreatedAt = createdAt
        };
        await _movements.AddAsync(movement);
        await _uow.SaveChangesAsync();
        return movement;
    }

    // ---------- Task 1: sales report ----------

    [Fact]
    public async Task Sales_report_totals_match_the_old_in_memory_figures()
    {
        await SeedThreeDaysOfSalesAsync();

        var report = await new GetSalesReportQueryHandler(_sales)
            .Handle(new GetSalesReportQuery(null, null, null, null), CancellationToken.None);

        Assert.Equal(510m, report.GrossSales);
        Assert.Equal(10m, report.TotalDiscounts);
        Assert.Equal(50m, report.TotalRefunds);
        Assert.Equal(450m, report.NetSales);
        Assert.Equal(4, report.TransactionCount);
    }

    [Fact]
    public async Task Sales_report_daily_rows_page_in_date_order_and_carry_per_day_refunds()
    {
        await SeedThreeDaysOfSalesAsync();
        var handler = new GetSalesReportQueryHandler(_sales);

        var first = await handler.Handle(new GetSalesReportQuery(null, null, 1, 2), CancellationToken.None);
        Assert.Equal(2, first.Items.Count);
        Assert.Equal(3, first.TotalCount);
        Assert.Equal(Day1.Date, first.Items[0].Date.Date);
        Assert.Equal(50m, first.Items[1].Refunds);
        Assert.Equal(150m, first.Items[1].NetSales);
        Assert.Equal(2, first.Items[0].TransactionCount);

        var second = await handler.Handle(new GetSalesReportQuery(null, null, 2, 2), CancellationToken.None);
        Assert.Single(second.Items);
        Assert.Equal(Day3.Date, second.Items[0].Date.Date);
    }

    // ---------- Task 2: expense report ----------

    [Fact]
    public async Task Expense_report_totals_and_purchase_pages()
    {
        var rice = await SeedItemAsync("Rice 1kg", cost: 5m);
        var soap = await SeedItemAsync("Soap", cost: 4m);
        await SeedMovementAsync(rice, StockMovementType.AddStock, 2, 10m, null, Day1, "Supplier A");
        await SeedMovementAsync(soap, StockMovementType.AddStock, 5, 4m, null, Day2);
        await SeedMovementAsync(rice, StockMovementType.AddStock, 1, 7m, null, Day3);
        await SeedMovementAsync(soap, StockMovementType.AddStock, 9, null, null, Day3.AddHours(1));
        await SeedMovementAsync(rice, StockMovementType.Adjustment, -3, null, AdjustmentReason.Loss, Day2.AddHours(1));

        var report = await new GetExpenseReportQueryHandler(_movements)
            .Handle(new GetExpenseReportQuery(null, null, 1, 2), CancellationToken.None);

        Assert.Equal(2, report.Items.Count);
        Assert.Equal(3, report.TotalCount);
        Assert.Equal(Day3.Date, report.Items[0].Date.Date);
        Assert.Equal(1, report.Items[0].Quantity);
        Assert.Equal("Rice 1kg", report.Items[0].ItemName);
        Assert.Equal(47m, report.CostOfPurchases);
        Assert.Equal(15m, report.InventoryLoss);
        Assert.Equal(62m, report.TotalExpenses);
        Assert.Equal(2, report.Breakdown.Count);
        Assert.Equal(47m, report.Breakdown.Single(b => b.Category == "Cost of Purchases").Amount);
        Assert.Equal(15m, report.Breakdown.Single(b => b.Category == "Inventory Loss").Amount);
    }

    // ---------- Task 3: profit report ----------

    /// <summary>
    /// Rice (cost 10): 5 @ 90, 10 @ 200, refund −2 @ −50 → revenue 240, cost 130, profit 110.
    /// Soap (cost 4): 7 @ 150, 3 @ 60 → revenue 210, cost 40, profit 170.
    /// Sale totals are the Task 1 figures, so unfiltered net sales is still 450.
    /// </summary>
    private async Task<(Item Rice, Item Soap)> SeedTwoItemSalesAsync()
    {
        var rice = await SeedItemAsync("Rice 1kg", cost: 10m);
        var soap = await SeedItemAsync("Soap", cost: 4m);
        await SeedSaleAsync(Day1, 100m, 10m, 90m, lines: new Line(rice, 5, 90m));
        await SeedSaleAsync(Day1.AddHours(2), 150m, 0m, 150m, lines: new Line(soap, 7, 150m));
        var paid = await SeedSaleAsync(Day2, 200m, 0m, 200m, lines: new Line(rice, 10, 200m));
        await SeedSaleAsync(Day2.AddHours(1), 50m, 0m, -50m, paid.Id, new Line(rice, -2, -50m));
        await SeedSaleAsync(Day3, 60m, 0m, 60m, lines: new Line(soap, 3, 60m));
        return (rice, soap);
    }

    [Fact]
    public async Task Profit_report_unfiltered_uses_sales_totals_and_pages_items_by_profit()
    {
        var (_, soap) = await SeedTwoItemSalesAsync();

        var report = await new GetProfitReportQueryHandler(_sales, _movements)
            .Handle(new GetProfitReportQuery(null, null, null, null, 1, 1), CancellationToken.None);

        Assert.Equal(450m, report.NetSales);
        Assert.Equal(170m, report.CostOfGoodsSold);
        Assert.Equal(280m, report.GrossProfit);
        Assert.Equal(0m, report.InventoryLoss);
        Assert.Equal(62.22m, report.GrossMarginPercent);
        Assert.Single(report.Items);
        Assert.Equal(2, report.TotalCount);
        Assert.Equal(soap.Id, report.Items[0].ItemId);
        Assert.Equal(170m, report.Items[0].Profit);
        Assert.Equal(10, report.Items[0].QuantitySold);
        Assert.Equal("General", report.Items[0].CategoryName);
    }

    [Fact]
    public async Task Profit_report_filtered_by_item_uses_that_items_line_revenue()
    {
        var (rice, _) = await SeedTwoItemSalesAsync();

        var report = await new GetProfitReportQueryHandler(_sales, _movements)
            .Handle(new GetProfitReportQuery(null, null, null, rice.Id, null, null), CancellationToken.None);

        Assert.Equal(240m, report.NetSales);
        Assert.Equal(130m, report.CostOfGoodsSold);
        Assert.Equal(1, report.TotalCount);
        var row = Assert.Single(report.Items);
        Assert.Equal(rice.Id, row.ItemId);
        Assert.Equal(13, row.QuantitySold);
        Assert.Equal(110m, row.Profit);
    }

    [Fact]
    public async Task Profit_report_filtered_by_category_uses_that_categorys_lines_and_losses()
    {
        var drinksId = Guid.NewGuid();
        _ctx.Categories.Add(new Category { Id = drinksId, Name = "Drinks" });
        await _ctx.SaveChangesAsync();
        var rice = await SeedItemAsync("Rice 1kg", cost: 10m);
        var coke = new Item
        {
            Name = "Coke 1.5L",
            ItemCode = $"T{++_seq:D4}",
            CostPrice = 50m,
            SellingPrice = 75m,
            Stock = 100,
            CategoryId = drinksId
        };
        await _items.AddAsync(coke);
        await _uow.SaveChangesAsync();
        await SeedSaleAsync(Day1, 90m, 0m, 90m, lines: new Line(rice, 5, 90m));
        await SeedSaleAsync(Day2, 150m, 0m, 150m, lines: new Line(coke, 2, 150m));
        await SeedMovementAsync(rice, StockMovementType.Adjustment, -2, null, AdjustmentReason.Loss, Day2);
        await SeedMovementAsync(coke, StockMovementType.Adjustment, -1, null, AdjustmentReason.Damage, Day3);

        var report = await new GetProfitReportQueryHandler(_sales, _movements)
            .Handle(new GetProfitReportQuery(null, null, drinksId, null, null, null), CancellationToken.None);

        Assert.Equal(150m, report.NetSales);
        Assert.Equal(100m, report.CostOfGoodsSold);
        Assert.Equal(50m, report.InventoryLoss);
        Assert.Equal(0m, report.NetProfit);
        Assert.Equal(1, report.TotalCount);
        Assert.Equal(coke.Id, Assert.Single(report.Items).ItemId);
    }

    // ---------- Task 4: best sellers ----------

    private async Task<Invoice> SeedInvoiceAsync(Suki suki, DateTime createdAt, bool voided, params Line[] lines)
    {
        var invoice = new Invoice
        {
            InvoiceNumber = $"INV{++_seq:D4}",
            SukiId = suki.Id,
            ShiftId = _shift.Id,
            Subtotal = lines.Sum(l => l.Total),
            Total = lines.Sum(l => l.Total),
            IsVoided = voided,
            CreatedBy = _user.Id,
            CreatedAt = createdAt,
            Items = lines.Select(l => new InvoiceItem
            {
                ItemId = l.Item.Id,
                ItemName = l.Item.Name,
                UnitPrice = l.Item.SellingPrice,
                CostPrice = l.Item.CostPrice,
                Quantity = l.Qty,
                Total = l.Total,
                CreatedAt = createdAt
            }).ToList()
        };
        await _invoices.AddAsync(invoice);
        await _uow.SaveChangesAsync();
        return invoice;
    }

    [Fact]
    public async Task Best_sellers_add_invoice_quantities_but_take_revenue_from_sales_only()
    {
        var suki = new Suki { Name = "Aling Rosa", CreatedBy = _user.Id };
        _ctx.Sukis.Add(suki);
        await _ctx.SaveChangesAsync();
        var a = await SeedItemAsync("Item A", cost: 6m, price: 10m);
        var b = await SeedItemAsync("Item B", cost: 4m, price: 5m);
        await SeedSaleAsync(Day1, 30m, 0m, 30m, lines: new Line(a, 3, 30m));
        await SeedSaleAsync(Day1.AddHours(1), 20m, 0m, 20m, lines: new Line(b, 4, 20m));
        await SeedInvoiceAsync(suki, Day2, false, new Line(a, 2, 20m));
        await SeedInvoiceAsync(suki, Day2.AddHours(1), true, new Line(a, 9, 90m));

        var result = await new GetBestSellersQueryHandler(_sales, _invoices)
            .Handle(new GetBestSellersQuery(null, null, null, null), CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(a.Id, result.Items[0].ItemId);
        Assert.Equal(5, result.Items[0].QuantitySold);
        Assert.Equal(30m, result.Items[0].Revenue);
        Assert.Equal(12m, result.Items[0].Profit);
        Assert.Equal(40m, result.Items[0].MarginPercent);
        Assert.Equal(b.Id, result.Items[1].ItemId);
        Assert.Equal(4, result.Items[1].QuantitySold);
    }

    [Fact]
    public async Task Best_sellers_page_two_returns_the_remainder()
    {
        for (var i = 1; i <= 25; i++)
        {
            var item = await SeedItemAsync($"Item {i:D2}");
            await SeedSaleAsync(Day1.AddMinutes(i), 20m, 0m, 20m, lines: new Line(item, 1, 20m));
        }

        var result = await new GetBestSellersQueryHandler(_sales, _invoices)
            .Handle(new GetBestSellersQuery(null, null, 2, null), CancellationToken.None);

        Assert.Equal(5, result.Items.Count);
        Assert.Equal(25, result.TotalCount);
    }

    [Fact]
    public async Task Best_sellers_list_an_item_charged_on_invoices_but_never_sold()
    {
        var suki = new Suki { Name = "Mang Ben", CreatedBy = _user.Id };
        _ctx.Sukis.Add(suki);
        await _ctx.SaveChangesAsync();
        var a = await SeedItemAsync("Item A", cost: 6m, price: 10m);
        await SeedInvoiceAsync(suki, Day1, false, new Line(a, 4, 40m));

        var result = await new GetBestSellersQueryHandler(_sales, _invoices)
            .Handle(new GetBestSellersQuery(null, null, null, null), CancellationToken.None);

        var row = Assert.Single(result.Items);
        Assert.Equal(a.Id, row.ItemId);
        Assert.Equal("Item A", row.ItemName);
        Assert.Equal(4, row.QuantitySold);
        Assert.Equal(0m, row.Revenue);
        Assert.Equal(0m, row.Profit);
        Assert.Equal(0m, row.MarginPercent);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }
}
