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

public class ItemBarcodeTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _ctx;
    private readonly ItemRepository _items;
    private readonly CategoryRepository _categories;
    private readonly UnitOfWork _uow;
    private readonly Guid _categoryId = Guid.NewGuid();

    public ItemBarcodeTests()
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

    private static CreateItemCommand NewItem(string name, string? barcode)
        => new(name, null, null, barcode, 10m, 15m, 5, Guid.Empty, null);

    [Fact]
    public async Task Create_with_barcode_persists_it()
    {
        var id = await CreateHandler().Handle(
            NewItem("Kopiko Blanca", "4800361413480") with { CategoryId = _categoryId },
            CancellationToken.None);

        var item = await _ctx.Items.FindAsync(id);
        Assert.Equal("4800361413480", item!.Barcode);
    }

    [Fact]
    public async Task Create_normalizes_blank_barcode_to_null()
    {
        var id = await CreateHandler().Handle(
            NewItem("Eggs per pc", "   ") with { CategoryId = _categoryId },
            CancellationToken.None);

        var item = await _ctx.Items.FindAsync(id);
        Assert.Null(item!.Barcode);
    }

    [Fact]
    public async Task Create_with_taken_barcode_throws()
    {
        await CreateHandler().Handle(
            NewItem("Kopiko Blanca", "4800361413480") with { CategoryId = _categoryId },
            CancellationToken.None);

        await Assert.ThrowsAsync<DomainException>(() =>
            CreateHandler().Handle(
                NewItem("Kopiko Black", "4800361413480") with { CategoryId = _categoryId },
                CancellationToken.None));
    }

    [Fact]
    public async Task Update_to_taken_barcode_throws()
    {
        await CreateHandler().Handle(
            NewItem("Kopiko Blanca", "4800361413480") with { CategoryId = _categoryId },
            CancellationToken.None);
        var otherId = await CreateHandler().Handle(
            NewItem("Kopiko Black", "4800361413497") with { CategoryId = _categoryId },
            CancellationToken.None);

        var update = new UpdateItemCommand(
            otherId, "Kopiko Black", null, null, "4800361413480",
            10m, 15m, 5, _categoryId, true, null);

        await Assert.ThrowsAsync<DomainException>(() =>
            UpdateHandler().Handle(update, CancellationToken.None));
    }

    [Fact]
    public async Task Update_keeping_own_barcode_passes()
    {
        var id = await CreateHandler().Handle(
            NewItem("Kopiko Blanca", "4800361413480") with { CategoryId = _categoryId },
            CancellationToken.None);

        var update = new UpdateItemCommand(
            id, "Kopiko Blanca Twin", null, null, "4800361413480",
            11m, 16m, 5, _categoryId, true, null);

        await UpdateHandler().Handle(update, CancellationToken.None);

        var item = await _ctx.Items.FindAsync(id);
        Assert.Equal("Kopiko Blanca Twin", item!.Name);
        Assert.Equal("4800361413480", item.Barcode);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }
}
