using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Application.Inventory.Queries.GetStockLevels;
using POS.Domain.Entities;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;
using Xunit;

namespace POS.Infrastructure.Tests;

public class StockLevelsPagingTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _ctx;
    private readonly ItemRepository _items;
    private readonly CompositeItemRepository _composites;
    private readonly UnitOfWork _uow;
    private readonly Guid _categoryId = Guid.NewGuid();
    private int _codeSeq;

    public StockLevelsPagingTests()
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
        _uow = new UnitOfWork(_ctx);

        _ctx.Categories.Add(new Category { Id = _categoryId, Name = "General" });
        _ctx.SaveChanges();
    }

    private async Task SeedItemAsync(string name, bool tracksStock = true)
    {
        await _items.AddAsync(new Item
        {
            Name = name,
            ItemCode = $"T{++_codeSeq:D4}",
            Stock = 10,
            CostPrice = 5m,
            SellingPrice = 10m,
            CategoryId = _categoryId,
            TracksStock = tracksStock
        });
        await _uow.SaveChangesAsync();
    }

    [Fact]
    public async Task Stock_levels_page_and_total_count_only_tracked_items()
    {
        for (var i = 1; i <= 25; i++)
            await SeedItemAsync($"Item {i:D2}");
        await SeedItemAsync("Delivery Fee", tracksStock: false);
        await SeedItemAsync("Gift Wrap", tracksStock: false);
        await SeedItemAsync("Service Charge", tracksStock: false);

        var handler = new GetStockLevelsQueryHandler(_items, _composites);

        var first = await handler.Handle(new GetStockLevelsQuery(null, null), CancellationToken.None);
        Assert.Equal(20, first.Items.Count);
        Assert.Equal(25, first.TotalCount);
        Assert.Equal(2, first.TotalPages);

        var second = await handler.Handle(new GetStockLevelsQuery(2, null), CancellationToken.None);
        Assert.Equal(5, second.Items.Count);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }
}
