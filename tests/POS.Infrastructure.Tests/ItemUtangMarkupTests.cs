using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Application.Items.Commands.CreateItem;
using POS.Application.Items.Commands.UpdateItem;
using POS.Application.Items.Queries.GetItems;
using POS.Domain.Entities;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;
using Xunit;

namespace POS.Infrastructure.Tests;

public class ItemUtangMarkupTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _ctx;
    private readonly ItemRepository _items;
    private readonly CategoryRepository _categories;
    private readonly CompositeItemRepository _composites;
    private readonly UnitOfWork _uow;
    private readonly Guid _categoryId = Guid.NewGuid();

    public ItemUtangMarkupTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _ctx = new AppDbContext(options);
        _ctx.Database.EnsureCreated();

        _items = new ItemRepository(_ctx);
        _categories = new CategoryRepository(_ctx);
        _composites = new CompositeItemRepository(_ctx);
        _uow = new UnitOfWork(_ctx);

        _ctx.Categories.Add(new Category { Id = _categoryId, Name = "General" });
        _ctx.SaveChanges();
    }

    private CreateItemCommand NewItem(decimal? utangMarkup) => new(
        "Marlboro Stick", null, null, null, 3m, 4m, 5, _categoryId, utangMarkup, true);

    [Fact]
    public async Task Create_persists_a_null_utang_markup()
    {
        var handler = new CreateItemCommandHandler(_items, _categories, _uow);

        var id = await handler.Handle(NewItem(null), CancellationToken.None);

        var stored = await _ctx.Items.AsNoTracking().SingleAsync(i => i.Id == id);
        Assert.Null(stored.UtangMarkup);
    }

    [Fact]
    public async Task Create_persists_a_set_utang_markup()
    {
        var handler = new CreateItemCommandHandler(_items, _categories, _uow);

        var id = await handler.Handle(NewItem(1m), CancellationToken.None);

        var stored = await _ctx.Items.AsNoTracking().SingleAsync(i => i.Id == id);
        Assert.Equal(1m, stored.UtangMarkup);
    }

    [Fact]
    public async Task Update_can_clear_the_utang_markup_back_to_null()
    {
        var create = new CreateItemCommandHandler(_items, _categories, _uow);
        var id = await create.Handle(NewItem(1m), CancellationToken.None);

        var update = new UpdateItemCommandHandler(_items, _uow);
        await update.Handle(
            new UpdateItemCommand(id, "Marlboro Stick", null, null, null, 3m, 4m, 5, _categoryId, true, null, true),
            CancellationToken.None);

        var stored = await _ctx.Items.AsNoTracking().SingleAsync(i => i.Id == id);
        Assert.Null(stored.UtangMarkup);
    }

    [Fact]
    public async Task GetItems_returns_the_utang_markup()
    {
        var create = new CreateItemCommandHandler(_items, _categories, _uow);
        var id = await create.Handle(NewItem(1.50m), CancellationToken.None);

        var result = await new GetItemsQueryHandler(_items, _composites)
            .Handle(new GetItemsQuery(1, 50), CancellationToken.None);

        Assert.Equal(1.50m, result.Items.Single(i => i.Id == id).UtangMarkup);
    }

    [Fact]
    public void Create_validator_rejects_a_negative_utang_markup()
    {
        var result = new CreateItemCommandValidator().Validate(NewItem(-1m));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Create_validator_accepts_a_null_utang_markup()
    {
        var result = new CreateItemCommandValidator().Validate(NewItem(null));
        Assert.True(result.IsValid);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }
}
