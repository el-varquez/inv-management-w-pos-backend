using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Application.Inventory.Queries.GetInventoryValuation;
using POS.Domain.Entities;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;
using Xunit;

namespace POS.Infrastructure.Tests;

public class ValuationPagingTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _ctx;
    private readonly ItemRepository _items;
    private readonly UnitOfWork _uow;
    private readonly Guid _categoryId = Guid.NewGuid();
    private int _codeSeq;

    public ValuationPagingTests()
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

    private async Task SeedItemAsync(
        string name, int stock, decimal cost, bool isComposite = false, bool isActive = true)
    {
        await _items.AddAsync(new Item
        {
            Name = name,
            ItemCode = $"T{++_codeSeq:D4}",
            Stock = stock,
            CostPrice = cost,
            SellingPrice = cost * 2,
            CategoryId = _categoryId,
            IsComposite = isComposite,
            IsActive = isActive
        });
        await _uow.SaveChangesAsync();
    }

    [Fact]
    public async Task Valuation_totals_cover_every_page()
    {
        for (var i = 1; i <= 25; i++)
            await SeedItemAsync($"Item {i:D2}", stock: 2, cost: 10m);

        var handler = new GetInventoryValuationQueryHandler(_items);

        var first = await handler.Handle(new GetInventoryValuationQuery(null, null), CancellationToken.None);
        Assert.Equal(20, first.Items.Count);
        Assert.Equal(25, first.TotalCount);
        Assert.Equal(25, first.TotalItems);
        Assert.Equal(500m, first.TotalValue);

        var second = await handler.Handle(new GetInventoryValuationQuery(2, null), CancellationToken.None);
        Assert.Equal(5, second.Items.Count);
        Assert.Equal(500m, second.TotalValue);
    }

    [Fact]
    public async Task Valuation_excludes_composites_and_inactive_items()
    {
        await SeedItemAsync("Creamy Coffee", stock: 3, cost: 10m, isComposite: true);
        await SeedItemAsync("Old Soap", stock: 3, cost: 10m, isActive: false);
        await SeedItemAsync("Rice 1kg", stock: 3, cost: 10m);

        var result = await new GetInventoryValuationQueryHandler(_items)
            .Handle(new GetInventoryValuationQuery(null, null), CancellationToken.None);

        Assert.Equal(1, result.TotalItems);
        Assert.Equal(30m, result.TotalValue);
        Assert.Single(result.Items);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }
}
