using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Application.Inventory.Commands.CompleteInventoryCount;
using POS.Application.Inventory.Commands.CreateInventoryCount;
using POS.Application.Inventory.Commands.SaveInventoryCountProgress;
using POS.Application.Inventory.Queries.GetInventoryCount;
using POS.Application.Inventory.Queries.GetInventoryCountLines;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Domain.Exceptions;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;
using POS.Infrastructure.Tests.Fakes;
using Xunit;

namespace POS.Infrastructure.Tests;

public class InventoryCountLinesTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _ctx;
    private readonly ItemRepository _items;
    private readonly InventoryCountRepository _counts;
    private readonly StockMovementRepository _movements;
    private readonly UnitOfWork _uow;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _categoryId = Guid.NewGuid();
    private int _codeSeq;

    public InventoryCountLinesTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        _ctx = new AppDbContext(options);
        _ctx.Database.EnsureCreated();

        _items = new ItemRepository(_ctx);
        _counts = new InventoryCountRepository(_ctx);
        _movements = new StockMovementRepository(_ctx);
        _uow = new UnitOfWork(_ctx);

        _ctx.Categories.Add(new Category { Id = _categoryId, Name = "General" });
        _ctx.SaveChanges();
    }

    private FakeCurrentUser User() => new() { Id = _userId, Role = "Admin" };

    private async Task<Item> SeedItemAsync(string name, int stock)
    {
        var item = new Item
        {
            Name = name, ItemCode = $"T{++_codeSeq:D4}",
            CostPrice = 1m, SellingPrice = 10m, Stock = stock,
            CategoryId = _categoryId
        };
        await _items.AddAsync(item);
        await _uow.SaveChangesAsync();
        return item;
    }

    private async Task<List<Item>> SeedTwentyFiveItemsAsync()
    {
        var items = new List<Item>();
        for (var i = 1; i <= 25; i++)
            items.Add(await SeedItemAsync($"Item {i:D2}", stock: 10));
        return items;
    }

    private async Task<Guid> StartCountAsync() =>
        await new CreateInventoryCountCommandHandler(_counts, _items, _uow, User())
            .Handle(new CreateInventoryCountCommand(null), CancellationToken.None);

    private async Task SaveProgressAsync(Guid id, params (Guid itemId, int? qty)[] lines) =>
        await new SaveInventoryCountProgressCommandHandler(_counts, _uow)
            .Handle(new SaveInventoryCountProgressCommand(
                id, lines.Select(l => new CountLineInput(l.itemId, l.qty)).ToList()),
                CancellationToken.None);

    private async Task CompleteAsync(Guid id, params (Guid itemId, int? qty)[] lines) =>
        await new CompleteInventoryCountCommandHandler(_counts, _items, _movements, _uow, User())
            .Handle(new CompleteInventoryCountCommand(
                id, lines.Select(l => new CountLineInput(l.itemId, l.qty)).ToList()),
                CancellationToken.None);

    [Fact]
    public async Task Count_lines_page_in_item_name_order()
    {
        await SeedTwentyFiveItemsAsync();
        var id = await StartCountAsync();
        var handler = new GetInventoryCountLinesQueryHandler(_counts);

        var first = await handler.Handle(new GetInventoryCountLinesQuery(id, null, null), CancellationToken.None);
        Assert.Equal(20, first.Items.Count);
        Assert.Equal(25, first.TotalCount);
        Assert.Equal(
            first.Items.Select(l => l.ItemName).OrderBy(n => n, StringComparer.Ordinal),
            first.Items.Select(l => l.ItemName));

        var second = await handler.Handle(new GetInventoryCountLinesQuery(id, 2, null), CancellationToken.None);
        Assert.Equal(5, second.Items.Count);
        Assert.Equal("Item 21", second.Items[0].ItemName);
    }

    [Fact]
    public async Task Count_header_reports_line_stats()
    {
        var items = await SeedTwentyFiveItemsAsync();
        var id = await StartCountAsync();
        await SaveProgressAsync(id, (items[0].Id, 10), (items[1].Id, 10), (items[2].Id, 12));

        var dto = await new GetInventoryCountQueryHandler(_counts)
            .Handle(new GetInventoryCountQuery(id), CancellationToken.None);

        Assert.Equal(25, dto.LineCount);
        Assert.Equal(3, dto.CountedCount);
        Assert.Equal(1, dto.VarianceCount);
    }

    [Fact]
    public async Task Complete_applies_lines_saved_earlier_but_not_resubmitted()
    {
        var a = await SeedItemAsync("Item A", stock: 10);
        var id = await StartCountAsync();
        await SaveProgressAsync(id, (a.Id, 7));

        await CompleteAsync(id);

        var updated = await _items.GetByIdAsync(a.Id, CancellationToken.None);
        Assert.Equal(7, updated!.Stock);

        var movement = Assert.Single(await _movements.GetByItemIdAsync(a.Id, CancellationToken.None));
        Assert.Equal(StockMovementType.InventoryCount, movement.Type);
        Assert.Equal(-3, movement.Quantity);

        var count = await _counts.GetByIdAsync(id, CancellationToken.None);
        Assert.Equal(InventoryCountStatus.Completed, count!.Status);
    }

    [Fact]
    public async Task Complete_with_a_line_reset_to_null_leaves_that_item_untouched()
    {
        var a = await SeedItemAsync("Item A", stock: 10);
        var id = await StartCountAsync();
        await SaveProgressAsync(id, (a.Id, 7));

        await CompleteAsync(id, (a.Id, null));

        var updated = await _items.GetByIdAsync(a.Id, CancellationToken.None);
        Assert.Equal(10, updated!.Stock);
        Assert.Empty(await _movements.GetByItemIdAsync(a.Id, CancellationToken.None));

        var count = await _counts.GetByIdAsync(id, CancellationToken.None);
        Assert.Equal(InventoryCountStatus.Completed, count!.Status);
        Assert.Null(count.Lines.Single().ActualQty);
    }

    [Fact]
    public async Task Lines_for_unknown_count_throw_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new GetInventoryCountLinesQueryHandler(_counts)
                .Handle(new GetInventoryCountLinesQuery(Guid.NewGuid(), null, null), CancellationToken.None));
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }
}
