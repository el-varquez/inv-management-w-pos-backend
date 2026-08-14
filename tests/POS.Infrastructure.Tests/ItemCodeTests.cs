using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Application.Items.Commands.CreateItem;
using POS.Application.Items.Commands.UpdateItem;
using POS.Domain.Entities;
using POS.Domain.Exceptions;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;
using Xunit;

namespace POS.Infrastructure.Tests;

public class ItemCodeTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _ctx;
    private readonly ItemRepository _items;
    private readonly CategoryRepository _categories;
    private readonly UnitOfWork _uow;
    private readonly Guid _categoryId = Guid.NewGuid();

    public ItemCodeTests()
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
        _uow = new UnitOfWork(_ctx);

        _ctx.Categories.Add(new Category { Id = _categoryId, Name = "General" });
        _ctx.SaveChanges();
    }

    private CreateItemCommandHandler CreateHandler()
        => new(_items, _categories, _uow);

    private UpdateItemCommandHandler UpdateHandler()
        => new(_items, _uow);

    private CreateItemCommand NewItem(string name, string? itemCode = null, string? barcode = null)
        => new(name, null, itemCode, barcode, 10m, 15m, 5, _categoryId, null, true);

    [Fact]
    public async Task Create_with_blank_code_gets_00001()
    {
        var id = await CreateHandler().Handle(
            NewItem("Kopiko Black"), CancellationToken.None);

        var item = await _ctx.Items.FindAsync(id);
        Assert.Equal("00001", item!.ItemCode);
    }

    [Fact]
    public async Task Codes_auto_increment_across_creates()
    {
        await CreateHandler().Handle(NewItem("Kopiko Black"), CancellationToken.None);
        var secondId = await CreateHandler().Handle(
            NewItem("Kopiko Blanca"), CancellationToken.None);

        var second = await _ctx.Items.FindAsync(secondId);
        Assert.Equal("00002", second!.ItemCode);
    }

    [Fact]
    public async Task Custom_text_code_is_kept_and_trimmed()
    {
        var id = await CreateHandler().Handle(
            NewItem("Coke 1L bottle", itemCode: "  Coke 1L  "), CancellationToken.None);

        var item = await _ctx.Items.FindAsync(id);
        Assert.Equal("Coke 1L", item!.ItemCode);
    }

    [Fact]
    public async Task Auto_generation_ignores_custom_text_codes()
    {
        await CreateHandler().Handle(
            NewItem("Coke 1L bottle", itemCode: "Coke 1L"), CancellationToken.None);
        var id = await CreateHandler().Handle(
            NewItem("Kopiko Black"), CancellationToken.None);

        var item = await _ctx.Items.FindAsync(id);
        Assert.Equal("00001", item!.ItemCode);
    }

    [Fact]
    public async Task Auto_generation_continues_after_manual_numeric_code()
    {
        await CreateHandler().Handle(
            NewItem("Eggs per pc", itemCode: "00042"), CancellationToken.None);
        var id = await CreateHandler().Handle(
            NewItem("Kopiko Black"), CancellationToken.None);

        var item = await _ctx.Items.FindAsync(id);
        Assert.Equal("00043", item!.ItemCode);
    }

    [Fact]
    public async Task Create_with_taken_code_throws()
    {
        await CreateHandler().Handle(
            NewItem("Coke 1L bottle", itemCode: "Coke 1L"), CancellationToken.None);

        await Assert.ThrowsAsync<DomainException>(() =>
            CreateHandler().Handle(
                NewItem("Coke 1L glass", itemCode: "Coke 1L"), CancellationToken.None));
    }

    [Fact]
    public async Task Update_with_blank_code_assigns_fresh_number()
    {
        var id = await CreateHandler().Handle(
            NewItem("Kopiko Black"), CancellationToken.None);
        await CreateHandler().Handle(
            NewItem("Kopiko Blanca"), CancellationToken.None);

        var update = new UpdateItemCommand(
            id, "Kopiko Black", null, null, null,
            10m, 15m, 5, _categoryId, true, null, true);
        await UpdateHandler().Handle(update, CancellationToken.None);

        var item = await _ctx.Items.FindAsync(id);
        Assert.Equal("00003", item!.ItemCode);
    }

    [Fact]
    public async Task Update_to_taken_code_throws()
    {
        await CreateHandler().Handle(
            NewItem("Kopiko Black", itemCode: "KB-1"), CancellationToken.None);
        var otherId = await CreateHandler().Handle(
            NewItem("Kopiko Blanca"), CancellationToken.None);

        var update = new UpdateItemCommand(
            otherId, "Kopiko Blanca", null, "KB-1", null,
            10m, 15m, 5, _categoryId, true, null, true);

        await Assert.ThrowsAsync<DomainException>(() =>
            UpdateHandler().Handle(update, CancellationToken.None));
    }

    [Fact]
    public async Task Update_keeping_own_code_passes()
    {
        var id = await CreateHandler().Handle(
            NewItem("Kopiko Black", itemCode: "KB-1"), CancellationToken.None);

        var update = new UpdateItemCommand(
            id, "Kopiko Black Twin", null, "KB-1", null,
            11m, 16m, 5, _categoryId, true, null, true);
        await UpdateHandler().Handle(update, CancellationToken.None);

        var item = await _ctx.Items.FindAsync(id);
        Assert.Equal("Kopiko Black Twin", item!.Name);
        Assert.Equal("KB-1", item.ItemCode);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }
}
