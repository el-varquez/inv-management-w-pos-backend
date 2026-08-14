using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common;
using POS.Application.Items.Queries.GetItems;
using POS.Application.Items.Queries.GetSellableItems;
using POS.Application.Inventory.Queries.GetInventoryValuation;
using POS.Application.Inventory.Queries.GetLowStockItems;
using POS.Application.Inventory.Queries.GetStockLevels;
using POS.Domain.Entities;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;
using POS.Infrastructure.Tests.Fakes;
using Xunit;

namespace POS.Infrastructure.Tests;

public class CompositeStockTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _ctx;
    private readonly ItemRepository _items;
    private readonly CompositeItemRepository _composites;
    private readonly UnitOfWork _uow;
    private readonly Guid _categoryId = Guid.NewGuid();
    private int _codeSeq;

    public CompositeStockTests()
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

    private async Task<Item> SeedItemAsync(
        string name, int stock, decimal cost = 0m, int lowStockThreshold = 5, bool isComposite = false)
    {
        var item = new Item
        {
            Name = name,
            ItemCode = $"T{++_codeSeq:D4}",
            Stock = stock,
            CostPrice = cost,
            SellingPrice = 100m,
            LowStockThreshold = lowStockThreshold,
            CategoryId = _categoryId,
            IsComposite = isComposite
        };
        await _items.AddAsync(item);
        await _uow.SaveChangesAsync();
        return item;
    }

    private async Task LinkComponentAsync(Guid parentId, Guid componentId, decimal qty)
    {
        await _composites.AddAsync(new CompositeItem
        {
            ParentItemId = parentId,
            ComponentItemId = componentId,
            Quantity = qty
        });
        await _uow.SaveChangesAsync();
    }

    private async Task<Item> SeedCreamyCoffeeAsync()
    {
        var beans = await SeedItemAsync("Coffee Beans", stock: 8);
        var creamer = await SeedItemAsync("Creamer", stock: 5);
        var sugar = await SeedItemAsync("Sugar", stock: 20);
        var coffee = await SeedItemAsync("Creamy Coffee", stock: 0, isComposite: true);
        await LinkComponentAsync(coffee.Id, beans.Id, 1m);
        await LinkComponentAsync(coffee.Id, creamer.Id, 1m);
        await LinkComponentAsync(coffee.Id, sugar.Id, 1m);
        return coffee;
    }

    [Fact]
    public void Buildable_is_min_floor_over_components()
    {
        var components = new List<CompositeItem>
        {
            new() { Quantity = 2m, ComponentItem = new Item { Stock = 5 } },
            new() { Quantity = 1m, ComponentItem = new Item { Stock = 1 } }
        };

        Assert.Equal(1, CompositeStock.Buildable(components));
    }

    [Fact]
    public void Buildable_is_zero_when_no_components()
    {
        Assert.Equal(0, CompositeStock.Buildable(new List<CompositeItem>()));
    }

    [Fact]
    public async Task GetItems_reports_buildable_stock_for_composite()
    {
        var coffee = await SeedCreamyCoffeeAsync();

        var result = await new GetItemsQueryHandler(_items, _composites)
            .Handle(new GetItemsQuery(1, 50), CancellationToken.None);

        var dto = result.Items.Single(i => i.Id == coffee.Id);
        Assert.Equal(5, dto.Stock);
    }

    [Fact]
    public async Task GetSellableItems_reports_buildable_stock_for_composite()
    {
        var coffee = await SeedCreamyCoffeeAsync();

        var result = await new GetSellableItemsQueryHandler(_items, _composites)
            .Handle(new GetSellableItemsQuery(), CancellationToken.None);

        var dto = result.Single(i => i.Id == coffee.Id);
        Assert.Equal(5, dto.Stock);
    }

    [Fact]
    public async Task GetStockLevels_reports_buildable_stock_and_zero_value_for_composite()
    {
        var coffee = await SeedCreamyCoffeeAsync();

        var result = await new GetStockLevelsQueryHandler(_items, _composites)
            .Handle(new GetStockLevelsQuery(1, 50), CancellationToken.None);

        var dto = result.Items.Single(i => i.ItemId == coffee.Id);
        Assert.Equal(5, dto.Stock);
        Assert.Equal(0m, dto.StockValue);
    }

    [Fact]
    public async Task GetInventoryValuation_excludes_composites()
    {
        var coffee = await SeedCreamyCoffeeAsync();

        var result = await new GetInventoryValuationQueryHandler(_items)
            .Handle(new GetInventoryValuationQuery(), CancellationToken.None);

        Assert.DoesNotContain(result.Items, i => i.ItemId == coffee.Id);
    }

    [Fact]
    public async Task GetLowStockItems_excludes_composites()
    {
        var coffee = await SeedCreamyCoffeeAsync();

        var result = await new GetLowStockItemsQueryHandler(_items)
            .Handle(new GetLowStockItemsQuery(), CancellationToken.None);

        Assert.DoesNotContain(result, i => i.ItemId == coffee.Id);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }
}
