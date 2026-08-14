using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Application.Settings.Commands.UpdateStoreSettings;
using POS.Application.Settings.Queries.GetStoreSettings;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;
using POS.Infrastructure.Tests.Fakes;
using Xunit;

namespace POS.Infrastructure.Tests;

public class StoreSettingsTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _ctx;
    private readonly StoreSettingsRepository _settings;
    private readonly UnitOfWork _uow;

    public StoreSettingsTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _ctx = new AppDbContext(options);
        _ctx.Database.EnsureCreated();

        _settings = new StoreSettingsRepository(_ctx);
        _uow = new UnitOfWork(_ctx);
    }

    [Fact]
    public async Task Get_returns_defaults_when_no_row_exists()
    {
        var handler = new GetStoreSettingsQueryHandler(_settings);

        var result = await handler.Handle(new GetStoreSettingsQuery(), CancellationToken.None);

        Assert.Equal("My Store", result.StoreName);
        Assert.Equal(string.Empty, result.Address);
        Assert.Equal(string.Empty, result.ReceiptFooter);
        Assert.True(result.AcceptUtang);
    }

    [Fact]
    public async Task Update_creates_the_single_row()
    {
        var update = new UpdateStoreSettingsCommandHandler(_settings, _uow);

        await update.Handle(
            new UpdateStoreSettingsCommand("Aling Nena's", "123 Rizal St", "Salamat po!", true, 0m, false, null),
            CancellationToken.None);

        Assert.Equal(1, await _ctx.StoreSettings.CountAsync());
        var read = new GetStoreSettingsQueryHandler(_settings);
        var result = await read.Handle(new GetStoreSettingsQuery(), CancellationToken.None);
        Assert.Equal("Aling Nena's", result.StoreName);
    }

    [Fact]
    public async Task Update_twice_stays_single_row_and_overwrites()
    {
        var update = new UpdateStoreSettingsCommandHandler(_settings, _uow);

        await update.Handle(
            new UpdateStoreSettingsCommand("First", "A", "x", true, 0m, false, null),
            CancellationToken.None);
        await update.Handle(
            new UpdateStoreSettingsCommand("Second", "B", "y", false, 0m, false, null),
            CancellationToken.None);

        Assert.Equal(1, await _ctx.StoreSettings.CountAsync());
        var row = await _ctx.StoreSettings.SingleAsync();
        Assert.Equal("Second", row.StoreName);
        Assert.Equal("B", row.Address);
        Assert.False(row.AcceptUtang);
    }

    [Fact]
    public void Update_validator_rejects_blank_store_name()
    {
        var result = new UpdateStoreSettingsCommandValidator()
            .Validate(new UpdateStoreSettingsCommand("  ", "", "", true, 0m, false, null));
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Get_returns_zero_default_markup_when_no_row_exists()
    {
        var handler = new GetStoreSettingsQueryHandler(_settings);

        var result = await handler.Handle(new GetStoreSettingsQuery(), CancellationToken.None);

        Assert.Equal(0m, result.DefaultUtangMarkup);
    }

    [Fact]
    public async Task Update_persists_the_default_markup()
    {
        var update = new UpdateStoreSettingsCommandHandler(_settings, _uow);

        await update.Handle(
            new UpdateStoreSettingsCommand(
                "Aling Nena's", "123 Rizal St", "Salamat po!", true, 1m, false, null),
            CancellationToken.None);

        var read = new GetStoreSettingsQueryHandler(_settings);
        var result = await read.Handle(new GetStoreSettingsQuery(), CancellationToken.None);
        Assert.Equal(1m, result.DefaultUtangMarkup);
    }

    [Fact]
    public void Update_validator_rejects_a_negative_default_markup()
    {
        var result = new UpdateStoreSettingsCommandValidator()
            .Validate(new UpdateStoreSettingsCommand("Store", "", "", true, -1m, false, null));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Update_validator_rejects_more_than_two_decimal_places()
    {
        var result = new UpdateStoreSettingsCommandValidator()
            .Validate(new UpdateStoreSettingsCommand("Store", "", "", true, 1.005m, false, null));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Gcash_wallet_tracking_defaults_to_off()
    {
        var handler = new GetStoreSettingsQueryHandler(_settings);

        var result = await handler.Handle(new GetStoreSettingsQuery(), CancellationToken.None);

        Assert.False(result.TrackGcashWallet);
        Assert.Null(result.GcashFeeItemId);
    }

    [Fact]
    public async Task Update_persists_the_gcash_wallet_settings()
    {
        var feeItemId = Guid.NewGuid();
        var update = new UpdateStoreSettingsCommandHandler(_settings, _uow);

        await update.Handle(
            new UpdateStoreSettingsCommand(
                "Aling Nena's", "123 Rizal St", "Salamat po!", true, 0m, true, feeItemId),
            CancellationToken.None);

        var read = new GetStoreSettingsQueryHandler(_settings);
        var result = await read.Handle(new GetStoreSettingsQuery(), CancellationToken.None);

        Assert.True(result.TrackGcashWallet);
        Assert.Equal(feeItemId, result.GcashFeeItemId);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }
}
