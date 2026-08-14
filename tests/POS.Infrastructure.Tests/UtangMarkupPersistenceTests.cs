using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Domain.Entities;
using POS.Infrastructure.Persistence;
using Xunit;

namespace POS.Infrastructure.Tests;

public class UtangMarkupPersistenceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _ctx;
    private readonly Guid _categoryId = Guid.NewGuid();

    public UtangMarkupPersistenceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _ctx = new AppDbContext(options);
        _ctx.Database.EnsureCreated();

        _ctx.Categories.Add(new Category { Id = _categoryId, Name = "General" });
        _ctx.SaveChanges();
    }

    [Fact]
    public async Task Item_utang_markup_defaults_to_null_and_round_trips()
    {
        var item = new Item
        {
            Name = "Marlboro Stick",
            ItemCode = "T0001",
            SellingPrice = 4m,
            CategoryId = _categoryId
        };
        _ctx.Items.Add(item);
        await _ctx.SaveChangesAsync();

        var fresh = await _ctx.Items.AsNoTracking().SingleAsync(i => i.Id == item.Id);
        Assert.Null(fresh.UtangMarkup);

        item.UtangMarkup = 1.25m;
        await _ctx.SaveChangesAsync();

        var updated = await _ctx.Items.AsNoTracking().SingleAsync(i => i.Id == item.Id);
        Assert.Equal(1.25m, updated.UtangMarkup);
    }

    [Fact]
    public async Task Store_default_utang_markup_defaults_to_zero_and_round_trips()
    {
        var settings = new StoreSettings();
        _ctx.StoreSettings.Add(settings);
        await _ctx.SaveChangesAsync();

        var fresh = await _ctx.StoreSettings.AsNoTracking().SingleAsync();
        Assert.Equal(0m, fresh.DefaultUtangMarkup);

        settings.DefaultUtangMarkup = 1m;
        await _ctx.SaveChangesAsync();

        var updated = await _ctx.StoreSettings.AsNoTracking().SingleAsync();
        Assert.Equal(1m, updated.DefaultUtangMarkup);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }
}
