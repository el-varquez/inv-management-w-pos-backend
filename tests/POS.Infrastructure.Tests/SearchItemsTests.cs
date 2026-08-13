using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Application.Items.Queries.SearchItems;
using POS.Domain.Entities;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;
using Xunit;

namespace POS.Infrastructure.Tests;

public class SearchItemsTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _ctx;
    private readonly ItemRepository _items;
    private readonly CompositeItemRepository _composites;
    private readonly UnitOfWork _uow;
    private readonly Guid _categoryId = Guid.NewGuid();

    public SearchItemsTests()
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

    private SearchItemsQueryHandler Handler() => new(_items, _composites);

    private async Task<Item> SeedAsync(
        string name, string? barcode = null, string? sku = null,
        int stock = 10, bool isActive = true, bool isComposite = false)
    {
        var item = new Item
        {
            Name = name,
            Barcode = barcode,
            Sku = sku,
            Stock = stock,
            CostPrice = 10m,
            SellingPrice = 15m,
            CategoryId = _categoryId,
            IsActive = isActive,
            IsComposite = isComposite
        };
        await _items.AddAsync(item);
        await _uow.SaveChangesAsync();
        return item;
    }

    [Fact]
    public async Task Exact_barcode_match_ranks_first()
    {
        await SeedAsync("Item 4800 Special");            // name contains the term
        var exact = await SeedAsync("Coke Mismo", barcode: "4800");

        var result = await Handler().Handle(
            new SearchItemsQuery("4800"), CancellationToken.None);

        Assert.Equal(exact.Id, result.First().Id);
    }

    [Fact]
    public async Task Exact_sku_ranks_before_name_contains()
    {
        await SeedAsync("ACE Detergent");                 // name contains "ace"
        var skuHit = await SeedAsync("Zonrox Bleach", sku: "ACE");

        var result = await Handler().Handle(
            new SearchItemsQuery("ace"), CancellationToken.None);

        Assert.Equal(skuHit.Id, result.First().Id);
    }

    [Fact]
    public async Task Name_contains_is_case_insensitive()
    {
        var item = await SeedAsync("Kopiko Blanca Twin");

        var result = await Handler().Handle(
            new SearchItemsQuery("kopiko"), CancellationToken.None);

        Assert.Contains(result, r => r.Id == item.Id);
    }

    [Fact]
    public async Task Default_limit_is_10_and_limit_is_clamped_to_25()
    {
        for (var i = 0; i < 30; i++)
            await SeedAsync($"Lucky Me Pancit {i:D2}");

        var byDefault = await Handler().Handle(
            new SearchItemsQuery("Lucky Me"), CancellationToken.None);
        var clamped = await Handler().Handle(
            new SearchItemsQuery("Lucky Me", 100), CancellationToken.None);

        Assert.Equal(10, byDefault.Count);
        Assert.Equal(25, clamped.Count);
    }

    [Fact]
    public async Task Inactive_items_are_included_and_flagged()
    {
        var inactive = await SeedAsync("Sky Flakes", isActive: false);

        var result = await Handler().Handle(
            new SearchItemsQuery("Sky Flakes"), CancellationToken.None);

        var dto = result.Single(r => r.Id == inactive.Id);
        Assert.False(dto.IsActive);
    }

    [Fact]
    public async Task Composite_reports_buildable_stock()
    {
        var beans = await SeedAsync("Coffee Beans", stock: 8);
        var composite = await SeedAsync("Creamy Coffee", stock: 0, isComposite: true);
        await _composites.AddAsync(new CompositeItem
        {
            ParentItemId = composite.Id,
            ComponentItemId = beans.Id,
            Quantity = 2m
        });
        await _uow.SaveChangesAsync();

        var result = await Handler().Handle(
            new SearchItemsQuery("Creamy"), CancellationToken.None);

        var dto = result.Single(r => r.Id == composite.Id);
        Assert.True(dto.IsComposite);
        Assert.Equal(4, dto.Stock);   // floor(8 / 2)
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }
}
