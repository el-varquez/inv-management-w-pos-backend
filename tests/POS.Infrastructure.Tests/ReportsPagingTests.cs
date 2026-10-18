using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
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

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }
}
