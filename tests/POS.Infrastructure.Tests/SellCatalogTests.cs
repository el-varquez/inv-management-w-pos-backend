using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Application.Items.Queries.GetPopularItems;
using POS.Application.Items.Queries.SearchSellableItems;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;
using Xunit;

namespace POS.Infrastructure.Tests;

public class SellCatalogTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _ctx;
    private readonly ItemRepository _items;
    private readonly CompositeItemRepository _composites;
    private readonly TransactionRepository _transactions;
    private readonly UnitOfWork _uow;
    private readonly Guid _categoryId = Guid.NewGuid();
    private int _codeSeq;

    public SellCatalogTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _ctx = new AppDbContext(options);
        _ctx.Database.EnsureCreated();

        _items = new ItemRepository(_ctx);
        _composites = new CompositeItemRepository(_ctx);
        _transactions = new TransactionRepository(_ctx);
        _uow = new UnitOfWork(_ctx);

        _ctx.Categories.Add(new Category { Id = _categoryId, Name = "General" });
        _ctx.SaveChanges();
    }

    private async Task<Item> SeedItemAsync(
        string name, int stock = 10, decimal price = 25m, bool isActive = true,
        bool isComposite = false, bool tracksStock = true, string? barcode = null)
    {
        var item = new Item
        {
            Name = name,
            ItemCode = $"S{++_codeSeq:D4}",
            Barcode = barcode,
            CostPrice = 10m,
            SellingPrice = price,
            Stock = stock,
            CategoryId = _categoryId,
            IsActive = isActive,
            IsComposite = isComposite,
            TracksStock = tracksStock
        };
        await _items.AddAsync(item);
        await _uow.SaveChangesAsync();
        return item;
    }

    private async Task SoldAsync(Item item, int qty, int daysAgo)
    {
        var tx = new Transaction
        {
            ReceiptNumber = $"R-SEED-{item.ItemCode}-{daysAgo}-{qty}",
            Subtotal = item.SellingPrice * qty,
            Total = item.SellingPrice * qty,
            PaymentType = PaymentType.Cash,
            AmountTendered = item.SellingPrice * qty,
            CreatedBy = Guid.NewGuid(),
            Items =
            [
                new TransactionItem
                {
                    ItemId = item.Id,
                    ItemName = item.Name,
                    UnitPrice = item.SellingPrice,
                    CostPrice = item.CostPrice,
                    Quantity = qty,
                    Total = item.SellingPrice * qty
                }
            ]
        };
        _ctx.Transactions.Add(tx);
        await _ctx.SaveChangesAsync();
        tx.CreatedAt = DateTime.UtcNow.AddDays(-daysAgo);
        await _ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task Search_returns_empty_for_blank_term()
    {
        await SeedItemAsync("Coke Mismo 300ml");
        var handler = new SearchSellableItemsQueryHandler(_items, _composites);

        var result = await handler.Handle(new SearchSellableItemsQuery("   "), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Search_excludes_inactive_items()
    {
        await SeedItemAsync("Coke Mismo 300ml");
        await SeedItemAsync("Coke Zero 300ml", isActive: false);
        var handler = new SearchSellableItemsQueryHandler(_items, _composites);

        var result = await handler.Handle(new SearchSellableItemsQuery("coke"), CancellationToken.None);

        var hit = Assert.Single(result);
        Assert.Equal("Coke Mismo 300ml", hit.Name);
    }

    [Fact]
    public async Task Search_puts_barcode_exact_match_first()
    {
        await SeedItemAsync("4801981126712 lookalike");
        await SeedItemAsync("Coke Mismo 300ml", barcode: "4801981126712");
        var handler = new SearchSellableItemsQueryHandler(_items, _composites);

        var result = await handler.Handle(
            new SearchSellableItemsQuery("4801981126712"), CancellationToken.None);

        Assert.Equal("Coke Mismo 300ml", result[0].Name);
    }

    [Fact]
    public async Task Search_reports_buildable_stock_for_composites()
    {
        var component = await SeedItemAsync("Coffee sachet", stock: 9);
        var bundle = await SeedItemAsync("Coffee bundle", stock: 0, isComposite: true);
        _ctx.CompositeItems.Add(new CompositeItem
        {
            ParentItemId = bundle.Id,
            ComponentItemId = component.Id,
            Quantity = 3m
        });
        await _ctx.SaveChangesAsync();
        var handler = new SearchSellableItemsQueryHandler(_items, _composites);

        var result = await handler.Handle(new SearchSellableItemsQuery("bundle"), CancellationToken.None);

        var hit = Assert.Single(result);
        Assert.Equal(3, hit.Stock);
        Assert.True(hit.IsComposite);
    }

    [Fact]
    public async Task Popular_ranks_by_quantity_sold_in_the_last_seven_days()
    {
        var a = await SeedItemAsync("Pancit Canton");
        var b = await SeedItemAsync("Sky Flakes");
        await SoldAsync(a, 3, daysAgo: 1);
        await SoldAsync(b, 9, daysAgo: 2);
        var handler = new GetPopularItemsQueryHandler(_items, _composites, _transactions);

        var result = await handler.Handle(new GetPopularItemsQuery(), CancellationToken.None);

        Assert.Equal("Sky Flakes", result[0].Name);
        Assert.Equal(9, result[0].QuantitySold);
        Assert.Equal("Pancit Canton", result[1].Name);
    }

    [Fact]
    public async Task Popular_ignores_sales_older_than_seven_days()
    {
        var a = await SeedItemAsync("Pancit Canton");
        await SoldAsync(a, 5, daysAgo: 8);
        var handler = new GetPopularItemsQueryHandler(_items, _composites, _transactions);

        var result = await handler.Handle(new GetPopularItemsQuery(), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Popular_caps_at_eight_and_skips_inactive_items()
    {
        for (var i = 0; i < 9; i++)
        {
            var item = await SeedItemAsync($"Item {i}");
            await SoldAsync(item, 20 - i, daysAgo: 1);
        }
        var inactive = await SeedItemAsync("Gone item", isActive: false);
        await SoldAsync(inactive, 99, daysAgo: 1);
        var handler = new GetPopularItemsQueryHandler(_items, _composites, _transactions);

        var result = await handler.Handle(new GetPopularItemsQuery(), CancellationToken.None);

        Assert.Equal(8, result.Count);
        Assert.DoesNotContain(result, r => r.Name == "Gone item");
        Assert.Equal("Item 0", result[0].Name);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }
}
