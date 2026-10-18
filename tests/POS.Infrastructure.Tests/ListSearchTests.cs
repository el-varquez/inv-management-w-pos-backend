using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Application.Inventory.Commands.CreateInventoryCount;
using POS.Application.Inventory.Queries.GetInventoryCountLines;
using POS.Application.Inventory.Queries.GetInventoryHistory;
using POS.Application.Inventory.Queries.GetInventoryValuation;
using POS.Application.Inventory.Queries.GetLowStockItems;
using POS.Application.Inventory.Queries.GetStockLevels;
using POS.Application.Items.Queries.GetItems;
using POS.Application.Items.Queries.SearchItems;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;
using POS.Infrastructure.Tests.Fakes;
using Xunit;

namespace POS.Infrastructure.Tests;

/// <summary>The optional `search` parameter on every items and inventory list, applied in SQL.</summary>
public class ListSearchTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _ctx;
    private readonly ItemRepository _items;
    private readonly CompositeItemRepository _composites;
    private readonly StockMovementRepository _movements;
    private readonly InventoryCountRepository _counts;
    private readonly UnitOfWork _uow;
    private readonly FakeCurrentUser _user = new();
    private readonly Guid _generalId = Guid.NewGuid();
    private readonly Guid _drinksId = Guid.NewGuid();
    private int _seq;

    public ListSearchTests()
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
        _movements = new StockMovementRepository(_ctx);
        _counts = new InventoryCountRepository(_ctx);
        _uow = new UnitOfWork(_ctx);

        _ctx.Categories.Add(new Category { Id = _generalId, Name = "General" });
        _ctx.Categories.Add(new Category { Id = _drinksId, Name = "Drinks" });
        _ctx.SaveChanges();
    }

    private async Task<Item> SeedItemAsync(
        string name,
        string? barcode = null,
        Guid? categoryId = null,
        int stock = 10,
        decimal cost = 5m,
        string? itemCode = null)
    {
        var item = new Item
        {
            Name = name,
            ItemCode = itemCode ?? $"T{++_seq:D4}",
            Barcode = barcode,
            Stock = stock,
            LowStockThreshold = 5,
            CostPrice = cost,
            SellingPrice = cost * 2,
            CategoryId = categoryId ?? _generalId
        };
        await _items.AddAsync(item);
        await _uow.SaveChangesAsync();
        return item;
    }

    /// <summary>Lucky Me (General), Kopiko (Drinks, low), Coke (Drinks), Bear Brand (General, code ACE-01).</summary>
    private async Task<(Item LuckyMe, Item Kopiko, Item Coke, Item BearBrand)> SeedCatalogAsync()
    {
        var luckyMe = await SeedItemAsync("Lucky Me Pancit Canton", "4800016644207", _generalId, stock: 10, cost: 9.5m);
        var kopiko = await SeedItemAsync("Kopiko Black", "8996001320211", _drinksId, stock: 2, cost: 6.5m);
        var coke = await SeedItemAsync("Coke 1.5L", null, _drinksId, stock: 12, cost: 62m);
        var bearBrand = await SeedItemAsync("Bear Brand", "4800361379014", _generalId, stock: 60, cost: 12.5m, itemCode: "ACE-01");
        return (luckyMe, kopiko, coke, bearBrand);
    }

    private GetItemsQueryHandler ItemsHandler() => new(_items, _composites);

    [Fact]
    public async Task Blank_search_is_no_filter()
    {
        await SeedCatalogAsync();

        var all = await ItemsHandler().Handle(new GetItemsQuery(null, null, null, null), CancellationToken.None);
        var blank = await ItemsHandler().Handle(new GetItemsQuery(null, null, null, "   "), CancellationToken.None);

        Assert.Equal(4, all.TotalCount);
        Assert.Equal(all.TotalCount, blank.TotalCount);
    }

    [Fact]
    public async Task Exact_barcode_matches_even_when_name_does_not()
    {
        var (_, kopiko, _, _) = await SeedCatalogAsync();

        var result = await ItemsHandler().Handle(
            new GetItemsQuery(null, null, null, "8996001320211"), CancellationToken.None);

        var only = Assert.Single(result.Items);
        Assert.Equal(kopiko.Id, only.Id);
    }

    [Fact]
    public async Task Items_search_matches_name_or_code_case_insensitively_and_pages()
    {
        var (_, _, _, bearBrand) = await SeedCatalogAsync();
        for (var i = 1; i <= 25; i++)
            await SeedItemAsync($"Bulk Item {i:D2}");

        var byName = await ItemsHandler().Handle(new GetItemsQuery(null, null, null, "BULK"), CancellationToken.None);
        Assert.Equal(20, byName.Items.Count);
        Assert.Equal(25, byName.TotalCount);
        Assert.Equal(2, byName.TotalPages);

        var byCode = await ItemsHandler().Handle(new GetItemsQuery(null, null, null, "ace"), CancellationToken.None);
        var only = Assert.Single(byCode.Items);
        Assert.Equal(bearBrand.Id, only.Id);
    }

    [Fact]
    public async Task Stock_levels_low_stock_and_valuation_honour_search()
    {
        var (_, kopiko, _, _) = await SeedCatalogAsync();

        var levels = await new GetStockLevelsQueryHandler(_items, _composites)
            .Handle(new GetStockLevelsQuery(null, null, "kopiko"), CancellationToken.None);
        Assert.Equal(1, levels.TotalCount);
        Assert.Equal(kopiko.Id, levels.Items[0].ItemId);

        var low = await new GetLowStockItemsQueryHandler(_items)
            .Handle(new GetLowStockItemsQuery(null, null, "kopiko"), CancellationToken.None);
        Assert.Equal(1, low.TotalCount);
        var healthy = await new GetLowStockItemsQueryHandler(_items)
            .Handle(new GetLowStockItemsQuery(null, null, "coke"), CancellationToken.None);
        Assert.Equal(0, healthy.TotalCount);

        var valuation = await new GetInventoryValuationQueryHandler(_items)
            .Handle(new GetInventoryValuationQuery(null, null, "kopiko"), CancellationToken.None);
        Assert.Equal(1, valuation.TotalItems);
        Assert.Equal(13m, valuation.TotalValue);
    }

    [Fact]
    public async Task History_search_matches_the_movement_item()
    {
        var (_, kopiko, coke, _) = await SeedCatalogAsync();
        foreach (var item in new[] { kopiko, coke })
        {
            await _movements.AddAsync(new StockMovement
            {
                ItemId = item.Id,
                Type = StockMovementType.AddStock,
                Quantity = 5,
                CostPerUnit = 1m,
                CreatedBy = _user.Id
            });
        }
        await _uow.SaveChangesAsync();

        var result = await new GetInventoryHistoryQueryHandler(_movements)
            .Handle(new GetInventoryHistoryQuery(null, null, null, null, null, "kopiko"), CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(kopiko.Id, result.Items[0].ItemId);
    }

    [Fact]
    public async Task Count_lines_search_matches_the_line_item()
    {
        var (_, kopiko, _, _) = await SeedCatalogAsync();
        var countId = await new CreateInventoryCountCommandHandler(_counts, _items, _uow, _user)
            .Handle(new CreateInventoryCountCommand(null), CancellationToken.None);

        var result = await new GetInventoryCountLinesQueryHandler(_counts)
            .Handle(new GetInventoryCountLinesQuery(countId, null, null, "KOPIKO"), CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(kopiko.Id, result.Items[0].ItemId);
    }

    [Fact]
    public async Task Typeahead_category_filter_narrows_results()
    {
        await SeedCatalogAsync();
        var handler = new SearchItemsQueryHandler(_items, _composites);

        var all = await handler.Handle(new SearchItemsQuery("k"), CancellationToken.None);
        var drinks = await handler.Handle(new SearchItemsQuery("k", null, _drinksId), CancellationToken.None);

        Assert.Equal(3, all.Count);
        Assert.Equal(2, drinks.Count);
        Assert.All(drinks, d => Assert.Equal("Drinks", d.CategoryName));
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }
}
