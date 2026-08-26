using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Application.Categories.Commands.CreateCategory;
using POS.Application.Categories.Commands.DeleteCategory;
using POS.Application.Categories.Commands.UpdateCategory;
using POS.Application.Categories.Queries.GetCategories;
using POS.Domain.Entities;
using POS.Domain.Exceptions;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;
using Xunit;

namespace POS.Infrastructure.Tests;

public class SystemCategoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _ctx;
    private readonly CategoryRepository _categories;
    private readonly UnitOfWork _uow;

    public SystemCategoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _ctx = new AppDbContext(options);
        _ctx.Database.EnsureCreated();
        _categories = new CategoryRepository(_ctx);
        _uow = new UnitOfWork(_ctx);
    }

    [Fact]
    public async Task Seeder_inserts_both_system_categories_when_missing()
    {
        CategorySeeder.Seed(_ctx);

        var cats = await _ctx.Categories.AsNoTracking().ToListAsync();
        Assert.Equal(2, cats.Count);
        Assert.All(cats, c => Assert.True(c.IsSystem));
        Assert.Contains(cats, c => c.Name == CategoryNames.InventoryItem);
        Assert.Contains(cats, c => c.Name == CategoryNames.Service);
    }

    [Fact]
    public async Task Seeder_is_idempotent()
    {
        CategorySeeder.Seed(_ctx);
        CategorySeeder.Seed(_ctx);

        Assert.Equal(2, await _ctx.Categories.AsNoTracking().CountAsync());
    }

    [Fact]
    public async Task Seeder_stamps_an_existing_category_bearing_a_system_name()
    {
        _ctx.Categories.Add(new Category { Name = "Service", Description = "kept" });
        _ctx.SaveChanges();

        CategorySeeder.Seed(_ctx);

        var cats = await _ctx.Categories.AsNoTracking().ToListAsync();
        Assert.Equal(2, cats.Count);
        var service = Assert.Single(cats, c => c.Name == CategoryNames.Service);
        Assert.True(service.IsSystem);
        Assert.Equal("kept", service.Description);
    }

    [Fact]
    public async Task Create_refuses_a_duplicate_name_case_insensitively()
    {
        CategorySeeder.Seed(_ctx);
        var handler = new CreateCategoryCommandHandler(_categories, _uow);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new CreateCategoryCommand("service", null), CancellationToken.None));
        Assert.Equal("A category named \"service\" already exists.", ex.Message);
    }

    [Fact]
    public async Task Update_refuses_renaming_a_system_category()
    {
        CategorySeeder.Seed(_ctx);
        var service = _ctx.Categories.Single(c => c.Name == CategoryNames.Service);
        var handler = new UpdateCategoryCommandHandler(_categories, _uow);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new UpdateCategoryCommand(service.Id, "Fees", null), CancellationToken.None));
        Assert.Equal("\"Service\" is a system category — it can't be renamed.", ex.Message);
    }

    [Fact]
    public async Task Update_refuses_a_rename_that_collides_with_another_name()
    {
        CategorySeeder.Seed(_ctx);
        _ctx.Categories.Add(new Category { Name = "Snacks" });
        _ctx.SaveChanges();
        var snacks = _ctx.Categories.Single(c => c.Name == "Snacks");
        var handler = new UpdateCategoryCommandHandler(_categories, _uow);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new UpdateCategoryCommand(snacks.Id, "inventory item", null), CancellationToken.None));
        Assert.Equal("A category named \"inventory item\" already exists.", ex.Message);
    }

    [Fact]
    public async Task Update_allows_recasing_a_category_to_its_own_name()
    {
        _ctx.Categories.Add(new Category { Name = "Snacks" });
        _ctx.SaveChanges();
        var snacks = _ctx.Categories.Single(c => c.Name == "Snacks");
        var handler = new UpdateCategoryCommandHandler(_categories, _uow);

        await handler.Handle(new UpdateCategoryCommand(snacks.Id, "snacks", "small bites"), CancellationToken.None);

        var stored = await _ctx.Categories.AsNoTracking().SingleAsync(c => c.Id == snacks.Id);
        Assert.Equal("snacks", stored.Name);
    }

    [Fact]
    public async Task Delete_refuses_a_system_category_even_when_empty()
    {
        CategorySeeder.Seed(_ctx);
        var service = _ctx.Categories.Single(c => c.Name == CategoryNames.Service);
        var handler = new DeleteCategoryCommandHandler(_categories, _uow);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new DeleteCategoryCommand(service.Id), CancellationToken.None));
        Assert.Equal("\"Service\" is a system category — it can't be deleted.", ex.Message);
    }

    [Fact]
    public async Task Delete_still_removes_an_empty_custom_category()
    {
        _ctx.Categories.Add(new Category { Name = "Snacks" });
        _ctx.SaveChanges();
        var snacks = _ctx.Categories.Single(c => c.Name == "Snacks");
        var handler = new DeleteCategoryCommandHandler(_categories, _uow);

        await handler.Handle(new DeleteCategoryCommand(snacks.Id), CancellationToken.None);

        Assert.Empty(await _ctx.Categories.AsNoTracking().Where(c => c.Name == "Snacks").ToListAsync());
    }

    [Fact]
    public async Task Get_categories_exposes_is_system()
    {
        CategorySeeder.Seed(_ctx);
        _ctx.Categories.Add(new Category { Name = "Snacks" });
        _ctx.SaveChanges();
        var handler = new GetCategoriesQueryHandler(_categories);

        var result = await handler.Handle(new GetCategoriesQuery(), CancellationToken.None);

        Assert.True(result.Single(c => c.Name == CategoryNames.Service).IsSystem);
        Assert.False(result.Single(c => c.Name == "Snacks").IsSystem);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }
}
