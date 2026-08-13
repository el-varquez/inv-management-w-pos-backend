using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Application.Inventory.Commands.CompleteInventoryCount;
using POS.Application.Inventory.Commands.CreateInventoryCount;
using POS.Application.Inventory.Commands.SaveInventoryCountProgress;
using POS.Application.Inventory.Queries.GetInventoryCount;
using POS.Application.Inventory.Queries.GetInventoryCounts;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;
using POS.Infrastructure.Tests.Fakes;
using Xunit;

namespace POS.Infrastructure.Tests;

public class InventoryCountResumeTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _ctx;
    private readonly ItemRepository _items;
    private readonly InventoryCountRepository _counts;
    private readonly StockMovementRepository _movements;
    private readonly UnitOfWork _uow;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _categoryId = Guid.NewGuid();

    public InventoryCountResumeTests()
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

    private FakeCurrentUser User() =>
        new() { Id = _userId, Role = "Admin" };

    private async Task<Item> SeedItemAsync(string name, int stock)
    {
        var item = new Item
        {
            Name = name, CostPrice = 1m, SellingPrice = 10m, Stock = stock,
            CategoryId = _categoryId
        };
        await _items.AddAsync(item);
        await _uow.SaveChangesAsync();
        return item;
    }

    private async Task<Guid> StartCountAsync(string? notes = null) =>
        await new CreateInventoryCountCommandHandler(_counts, _items, _uow, User())
            .Handle(new CreateInventoryCountCommand(notes), CancellationToken.None);

    [Fact]
    public async Task New_draft_snapshots_expected_and_leaves_lines_uncounted()
    {
        await SeedItemAsync("Rice", stock: 12);

        var id = await StartCountAsync();

        var count = await _counts.GetByIdAsync(id, CancellationToken.None);
        Assert.NotNull(count);
        Assert.Equal(InventoryCountStatus.Draft, count!.Status);
        var line = Assert.Single(count.Lines);
        Assert.Equal(12, line.ExpectedQty);
        Assert.Null(line.ActualQty);
    }

    private async Task CompleteAsync(Guid id, params (Guid itemId, int? qty)[] lines) =>
        await new CompleteInventoryCountCommandHandler(_counts, _items, _movements, _uow, User())
            .Handle(new CompleteInventoryCountCommand(
                id, lines.Select(l => new CountLineInput(l.itemId, l.qty)).ToList()),
                CancellationToken.None);

    [Fact]
    public async Task Complete_records_movement_against_current_stock_not_start_snapshot()
    {
        var rice = await SeedItemAsync("Rice", stock: 10);
        var id = await StartCountAsync();

        rice.Stock = 8;
        await _items.UpdateAsync(rice);
        await _uow.SaveChangesAsync();

        await CompleteAsync(id, (rice.Id, 9));

        var updated = await _items.GetByIdAsync(rice.Id, CancellationToken.None);
        Assert.Equal(9, updated!.Stock);

        var movement = Assert.Single(
            await _movements.GetByItemIdAsync(rice.Id, CancellationToken.None));
        Assert.Equal(StockMovementType.InventoryCount, movement.Type);
        Assert.Equal(1, movement.Quantity);
    }

    [Fact]
    public async Task Complete_skips_uncounted_lines()
    {
        var rice = await SeedItemAsync("Rice", stock: 10);
        var soap = await SeedItemAsync("Soap", stock: 5);
        var id = await StartCountAsync();

        await CompleteAsync(id, (rice.Id, 7), (soap.Id, null));

        var updatedRice = await _items.GetByIdAsync(rice.Id, CancellationToken.None);
        var updatedSoap = await _items.GetByIdAsync(soap.Id, CancellationToken.None);
        Assert.Equal(7, updatedRice!.Stock);
        Assert.Equal(5, updatedSoap!.Stock);
        Assert.Empty(await _movements.GetByItemIdAsync(soap.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Complete_twice_is_rejected()
    {
        var rice = await SeedItemAsync("Rice", stock: 10);
        var id = await StartCountAsync();
        await CompleteAsync(id, (rice.Id, 10));

        await Assert.ThrowsAsync<POS.Domain.Exceptions.DomainException>(() =>
            CompleteAsync(id, (rice.Id, 11)));
    }

    private async Task SaveProgressAsync(Guid id, params (Guid itemId, int? qty)[] lines) =>
        await new SaveInventoryCountProgressCommandHandler(_counts, _uow)
            .Handle(new SaveInventoryCountProgressCommand(
                id, lines.Select(l => new CountLineInput(l.itemId, l.qty)).ToList()),
                CancellationToken.None);

    [Fact]
    public async Task SaveProgress_persists_actuals_without_touching_stock_or_status()
    {
        var rice = await SeedItemAsync("Rice", stock: 10);
        var id = await StartCountAsync();

        await SaveProgressAsync(id, (rice.Id, 7));

        var count = await _counts.GetByIdAsync(id, CancellationToken.None);
        Assert.Equal(InventoryCountStatus.Draft, count!.Status);
        Assert.Equal(7, count.Lines.Single().ActualQty);

        var stillTen = await _items.GetByIdAsync(rice.Id, CancellationToken.None);
        Assert.Equal(10, stillTen!.Stock);
        Assert.Empty(await _movements.GetByItemIdAsync(rice.Id, CancellationToken.None));
    }

    [Fact]
    public async Task SaveProgress_on_completed_count_is_rejected()
    {
        var rice = await SeedItemAsync("Rice", stock: 10);
        var id = await StartCountAsync();
        await CompleteAsync(id, (rice.Id, 10));

        await Assert.ThrowsAsync<POS.Domain.Exceptions.DomainException>(() =>
            SaveProgressAsync(id, (rice.Id, 9)));
    }

    [Fact]
    public async Task GetInventoryCount_returns_snapshot_and_saved_actuals()
    {
        var rice = await SeedItemAsync("Rice", stock: 10);
        var id = await StartCountAsync("Month end");
        await SaveProgressAsync(id, (rice.Id, 8));

        var dto = await new GetInventoryCountQueryHandler(_counts)
            .Handle(new GetInventoryCountQuery(id), CancellationToken.None);

        Assert.Equal("Month end", dto.Notes);
        Assert.Equal("Draft", dto.Status);
        var line = Assert.Single(dto.Lines);
        Assert.Equal("Rice", line.ItemName);
        Assert.Equal("General", line.CategoryName);
        Assert.Equal(10, line.ExpectedQty);
        Assert.Equal(8, line.ActualQty);
    }

    [Fact]
    public async Task GetInventoryCounts_lists_drafts_filtered_and_paged()
    {
        var rice = await SeedItemAsync("Rice", stock: 10);
        var draftId = await StartCountAsync();
        var toComplete = await StartCountAsync();
        await CompleteAsync(toComplete, (rice.Id, 10));

        var drafts = await new GetInventoryCountsQueryHandler(_counts)
            .Handle(new GetInventoryCountsQuery(InventoryCountStatus.Draft, 1, 20),
                CancellationToken.None);

        Assert.Equal(1, drafts.TotalCount);
        Assert.Equal(draftId, drafts.Items.Single().Id);
        Assert.Equal("Draft", drafts.Items.Single().Status);
        Assert.Equal(1, drafts.Items.Single().LineCount);

        var all = await new GetInventoryCountsQueryHandler(_counts)
            .Handle(new GetInventoryCountsQuery(null, 1, 20), CancellationToken.None);
        Assert.Equal(2, all.TotalCount);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }
}
