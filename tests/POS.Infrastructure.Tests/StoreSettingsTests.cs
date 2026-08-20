using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Application.Settings.Commands.UpdateStoreSettings;
using POS.Application.Settings.Queries.GetStoreName;
using POS.Application.Settings.Queries.GetStoreSettings;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Domain.Exceptions;
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
    private readonly ShiftRepository _shifts;
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
        _shifts = new ShiftRepository(_ctx);
        _uow = new UnitOfWork(_ctx);
    }

    private async Task SeedOpenShiftAsync()
    {
        var day = new BusinessDay
        {
            Number = 1,
            Status = DayStatus.Open,
            OpenedAt = DateTime.UtcNow,
            OpenedBy = Guid.NewGuid()
        };
        _ctx.BusinessDays.Add(day);
        _ctx.Shifts.Add(new Shift
        {
            Number = 1,
            Status = ShiftStatus.Open,
            StartingCash = 1000m,
            OpenedAt = DateTime.UtcNow,
            OpenedBy = Guid.NewGuid(),
            BusinessDayId = day.Id
        });
        await _ctx.SaveChangesAsync();
    }

    private static UpdateStoreSettingsCommand Command(bool trackEWalletFloat) =>
        new("My Store", "", "", true, 0m, trackEWalletFloat, null);

    [Fact]
    public async Task Turning_e_wallet_tracking_on_is_blocked_while_a_shift_is_open()
    {
        await SeedOpenShiftAsync();
        var update = new UpdateStoreSettingsCommandHandler(_settings, _shifts, _uow);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => update.Handle(Command(trackEWalletFloat: true), CancellationToken.None));

        Assert.Contains("still open", ex.Message);
    }

    [Fact]
    public async Task Turning_e_wallet_tracking_off_is_blocked_while_a_shift_is_open()
    {
        var update = new UpdateStoreSettingsCommandHandler(_settings, _shifts, _uow);
        await update.Handle(Command(trackEWalletFloat: true), CancellationToken.None);
        await SeedOpenShiftAsync();

        await Assert.ThrowsAsync<DomainException>(
            () => update.Handle(Command(trackEWalletFloat: false), CancellationToken.None));
    }

    [Fact]
    public async Task Unchanged_e_wallet_tracking_still_saves_while_a_shift_is_open()
    {
        var update = new UpdateStoreSettingsCommandHandler(_settings, _shifts, _uow);
        await update.Handle(Command(trackEWalletFloat: true), CancellationToken.None);
        await SeedOpenShiftAsync();

        await update.Handle(
            new UpdateStoreSettingsCommand("Renamed Store", "", "", true, 0m, true, null),
            CancellationToken.None);

        var read = new GetStoreSettingsQueryHandler(_settings);
        var result = await read.Handle(new GetStoreSettingsQuery(), CancellationToken.None);
        Assert.Equal("Renamed Store", result.StoreName);
        Assert.True(result.TrackEWalletFloat);
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
        var update = new UpdateStoreSettingsCommandHandler(_settings, _shifts, _uow);

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
        var update = new UpdateStoreSettingsCommandHandler(_settings, _shifts, _uow);

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
        var update = new UpdateStoreSettingsCommandHandler(_settings, _shifts, _uow);

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
    public async Task E_wallet_float_tracking_defaults_to_off()
    {
        var handler = new GetStoreSettingsQueryHandler(_settings);

        var result = await handler.Handle(new GetStoreSettingsQuery(), CancellationToken.None);

        Assert.False(result.TrackEWalletFloat);
        Assert.Null(result.EWalletFeeItemId);
    }

    [Fact]
    public async Task Update_persists_the_e_wallet_float_settings()
    {
        var feeItemId = Guid.NewGuid();
        var update = new UpdateStoreSettingsCommandHandler(_settings, _shifts, _uow);

        await update.Handle(
            new UpdateStoreSettingsCommand(
                "Aling Nena's", "123 Rizal St", "Salamat po!", true, 0m, true, feeItemId),
            CancellationToken.None);

        var read = new GetStoreSettingsQueryHandler(_settings);
        var result = await read.Handle(new GetStoreSettingsQuery(), CancellationToken.None);

        Assert.True(result.TrackEWalletFloat);
        Assert.Equal(feeItemId, result.EWalletFeeItemId);
    }

    [Fact]
    public async Task StoreName_returns_default_when_no_row_exists()
    {
        var handler = new GetStoreNameQueryHandler(_settings);

        var result = await handler.Handle(new GetStoreNameQuery(), CancellationToken.None);

        Assert.Equal("My Store", result.StoreName);
    }

    [Fact]
    public async Task StoreName_returns_saved_value()
    {
        var update = new UpdateStoreSettingsCommandHandler(_settings, _shifts, _uow);
        await update.Handle(
            new UpdateStoreSettingsCommand("Aling Nena's", "", "", true, 0m, false, null),
            CancellationToken.None);

        var handler = new GetStoreNameQueryHandler(_settings);
        var result = await handler.Handle(new GetStoreNameQuery(), CancellationToken.None);

        Assert.Equal("Aling Nena's", result.StoreName);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }
}
