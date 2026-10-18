using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Application.Inventory.Queries.GetLowStockItems;
using POS.Domain.Entities;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;
using Xunit;

namespace POS.Infrastructure.Tests;

public class LowStockPagingTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _ctx;
    private readonly ItemRepository _items;
    private readonly UnitOfWork _uow;
    private readonly Guid _categoryId = Guid.NewGuid();
    private int _codeSeq;

    public LowStockPagingTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _ctx = new AppDbContext(options);
        _ctx.Database.EnsureCreated();

        _items = new ItemRepository(_ctx);
        _uow = new UnitOfWork(_ctx);

        _ctx.Categories.Add(new Category { Id = _categoryId, Name = "General" });
        _ctx.SaveChanges();
    }

    private async Task<Item> SeedItemAsync(
        string name,
        int stock = 0,
        int threshold = 5,
        bool tracksStock = true,
        bool isComposite = false)
    {
        var item = new Item
        {
            Name = name,
            ItemCode = $"T{++_codeSeq:D4}",
            Stock = stock,
            LowStockThreshold = threshold,
            CostPrice = 5m,
            SellingPrice = 10m,
            CategoryId = _categoryId,
            TracksStock = tracksStock,
            IsComposite = isComposite
        };
        await _items.AddAsync(item);
        await _uow.SaveChangesAsync();
        return item;
    }

    [Fact]
    public async Task Low_stock_pages_are_disjoint_ordered_by_name_and_sum_to_the_total()
    {
        for (var i = 1; i <= 25; i++)
            await SeedItemAsync($"Item {i:D2}");

        var handler = new GetLowStockItemsQueryHandler(_items);

        var first = await handler.Handle(new GetLowStockItemsQuery(null, null), CancellationToken.None);
        Assert.Equal(20, first.Items.Count);
        Assert.Equal(25, first.TotalCount);
        Assert.Equal(2, first.TotalPages);
        Assert.Equal(first.Items.Select(i => i.ItemName).OrderBy(n => n, StringComparer.Ordinal), first.Items.Select(i => i.ItemName));

        var second = await handler.Handle(new GetLowStockItemsQuery(2, null), CancellationToken.None);
        Assert.Equal(5, second.Items.Count);

        var ids = first.Items.Select(i => i.ItemId).Concat(second.Items.Select(i => i.ItemId)).ToList();
        Assert.Equal(25, ids.Distinct().Count());
    }

    [Fact]
    public async Task Low_stock_excludes_composites_non_tracking_and_healthy_items()
    {
        await SeedItemAsync("Creamy Coffee", isComposite: true);
        await SeedItemAsync("Delivery Fee", tracksStock: false);
        await SeedItemAsync("Rice 1kg", stock: 10, threshold: 5);
        var low = await SeedItemAsync("Sugar 1kg", stock: 0, threshold: 5);

        var result = await new GetLowStockItemsQueryHandler(_items)
            .Handle(new GetLowStockItemsQuery(null, null), CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        var only = Assert.Single(result.Items);
        Assert.Equal(low.Id, only.ItemId);
        Assert.Equal(5, only.Deficit);
    }

    [Fact]
    public async Task Page_past_the_end_returns_no_items_but_keeps_the_total()
    {
        await SeedItemAsync("Item 01");
        await SeedItemAsync("Item 02");
        await SeedItemAsync("Item 03");

        var result = await new GetLowStockItemsQueryHandler(_items)
            .Handle(new GetLowStockItemsQuery(5, 2), CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }
}
