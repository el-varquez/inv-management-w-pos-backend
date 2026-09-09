using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Application.Settings.Commands.SetAcceptUtang;
using POS.Application.Settings.Commands.UpdateStoreSettings;
using POS.Application.Settings.Queries.GetStoreName;
using POS.Application.Settings.Queries.GetStoreSettings;
using POS.Application.Utang.Queries.GetUtangOutstanding;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Domain.Exceptions;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;
using POS.Infrastructure.Services;
using Xunit;

namespace POS.Infrastructure.Tests;

public class StoreSettingsTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _ctx;
    private readonly StoreSettingsRepository _settings;
    private readonly UtangRepository _utang;
    private readonly UserRepository _users;
    private readonly PasswordHasher _hasher = new();
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
        _utang = new UtangRepository(_ctx);
        _users = new UserRepository(_ctx);
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
    }

    [Fact]
    public async Task Update_creates_the_single_row()
    {
        var update = new UpdateStoreSettingsCommandHandler(_settings, _uow);

        await update.Handle(
            new UpdateStoreSettingsCommand("Aling Nena's", "123 Rizal St", "Salamat po!", 0m),
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
            new UpdateStoreSettingsCommand("First", "A", "x", 0m),
            CancellationToken.None);
        await update.Handle(
            new UpdateStoreSettingsCommand("Second", "B", "y", 0m),
            CancellationToken.None);

        Assert.Equal(1, await _ctx.StoreSettings.CountAsync());
        var row = await _ctx.StoreSettings.SingleAsync();
        Assert.Equal("Second", row.StoreName);
        Assert.Equal("B", row.Address);
    }

    [Fact]
    public void Update_validator_rejects_blank_store_name()
    {
        var result = new UpdateStoreSettingsCommandValidator()
            .Validate(new UpdateStoreSettingsCommand("  ", "", "", 0m));
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
                "Aling Nena's", "123 Rizal St", "Salamat po!", 1m),
            CancellationToken.None);

        var read = new GetStoreSettingsQueryHandler(_settings);
        var result = await read.Handle(new GetStoreSettingsQuery(), CancellationToken.None);
        Assert.Equal(1m, result.DefaultUtangMarkup);
    }

    [Fact]
    public void Update_validator_rejects_a_negative_default_markup()
    {
        var result = new UpdateStoreSettingsCommandValidator()
            .Validate(new UpdateStoreSettingsCommand("Store", "", "", -1m));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Update_validator_rejects_more_than_two_decimal_places()
    {
        var result = new UpdateStoreSettingsCommandValidator()
            .Validate(new UpdateStoreSettingsCommand("Store", "", "", 1.005m));

        Assert.False(result.IsValid);
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
        var update = new UpdateStoreSettingsCommandHandler(_settings, _uow);
        await update.Handle(
            new UpdateStoreSettingsCommand("Aling Nena's", "", "", 0m),
            CancellationToken.None);

        var handler = new GetStoreNameQueryHandler(_settings);
        var result = await handler.Handle(new GetStoreNameQuery(), CancellationToken.None);

        Assert.Equal("Aling Nena's", result.StoreName);
    }

    [Fact]
    public async Task Get_defaults_utang_to_off_with_seven_reminder_days()
    {
        var handler = new GetStoreSettingsQueryHandler(_settings);

        var result = await handler.Handle(new GetStoreSettingsQuery(), CancellationToken.None);

        Assert.False(result.AcceptUtang);
        Assert.Equal(7, result.UtangReminderDays);
    }

    [Fact]
    public async Task Update_persists_reminder_days_and_leaves_accept_utang_alone()
    {
        var update = new UpdateStoreSettingsCommandHandler(_settings, _uow);

        await update.Handle(
            new UpdateStoreSettingsCommand("Store", "", "", 0m, 30),
            CancellationToken.None);

        var row = await _ctx.StoreSettings.SingleAsync();
        Assert.Equal(30, row.UtangReminderDays);
        Assert.False(row.AcceptUtang);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(366)]
    public void Reminder_days_outside_one_to_365_are_refused(int days)
    {
        var result = new UpdateStoreSettingsCommandValidator()
            .Validate(new UpdateStoreSettingsCommand("Store", "", "", 0m, days));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Reminder days must be between 1 and 365.");
    }

    private SetAcceptUtangCommandHandler ToggleHandler()
        => new(_settings, _utang, _users, _hasher, _uow);

    private async Task SeedOwingSukiAsync(decimal invoiced, decimal paid = 0m)
    {
        var suki = new Suki { Name = "Aling Rosa", CreatedBy = Guid.NewGuid() };
        var shift = new Shift
        {
            Number = 1,
            Status = ShiftStatus.Open,
            OpenedAt = DateTime.UtcNow,
            OpenedBy = Guid.NewGuid(),
            BusinessDay = new BusinessDay
            {
                Number = 1,
                Status = DayStatus.Open,
                OpenedAt = DateTime.UtcNow,
                OpenedBy = Guid.NewGuid()
            }
        };
        _ctx.Sukis.Add(suki);
        _ctx.Shifts.Add(shift);
        _ctx.Invoices.Add(new Invoice
        {
            InvoiceNumber = "INV-TEST-0001",
            SukiId = suki.Id,
            ShiftId = shift.Id,
            Subtotal = invoiced,
            Total = invoiced,
            CreatedBy = Guid.NewGuid()
        });
        if (paid > 0m)
            _ctx.Payments.Add(new Payment { SukiId = suki.Id, Amount = paid, CreatedBy = Guid.NewGuid() });
        _ctx.StoreSettings.Add(new StoreSettings { StoreName = "S", AcceptUtang = true });
        await _ctx.SaveChangesAsync();
    }

    private async Task SeedUserAsync(string username, string role, string password)
    {
        _ctx.Users.Add(new User
        {
            Name = username,
            Username = username,
            Role = role,
            PasswordHash = _hasher.Hash(password),
            IsActive = true
        });
        await _ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task Turning_utang_on_needs_no_credentials()
    {
        await ToggleHandler().Handle(new SetAcceptUtangCommand(true), CancellationToken.None);

        Assert.True((await _ctx.StoreSettings.SingleAsync()).AcceptUtang);
    }

    [Fact]
    public async Task Turning_utang_off_with_nothing_owed_needs_no_credentials()
    {
        await SeedOwingSukiAsync(100m, paid: 100m);

        await ToggleHandler().Handle(new SetAcceptUtangCommand(false), CancellationToken.None);

        Assert.False((await _ctx.StoreSettings.SingleAsync()).AcceptUtang);
    }

    [Fact]
    public async Task Turning_utang_off_with_money_owed_and_no_credentials_is_refused()
    {
        await SeedOwingSukiAsync(100m);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => ToggleHandler().Handle(new SetAcceptUtangCommand(false), CancellationToken.None));

        Assert.Equal("Sukis still owe money — confirm with admin credentials to turn utang off.", ex.Message);
        Assert.True((await _ctx.StoreSettings.SingleAsync()).AcceptUtang);
    }

    [Fact]
    public async Task Turning_utang_off_with_wrong_credentials_is_refused_with_the_single_message()
    {
        await SeedOwingSukiAsync(100m);
        await SeedUserAsync("admin", "Admin", "right");

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => ToggleHandler().Handle(new SetAcceptUtangCommand(false, "admin", "wrong"), CancellationToken.None));

        Assert.Equal("Those credentials don't belong to an active admin account.", ex.Message);
    }

    [Fact]
    public async Task Turning_utang_off_with_cashier_credentials_is_refused()
    {
        await SeedOwingSukiAsync(100m);
        await SeedUserAsync("jomar", "Cashier", "pw");

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => ToggleHandler().Handle(new SetAcceptUtangCommand(false, "jomar", "pw"), CancellationToken.None));

        Assert.Equal("Those credentials don't belong to an active admin account.", ex.Message);
    }

    [Fact]
    public async Task Turning_utang_off_with_admin_credentials_succeeds()
    {
        await SeedOwingSukiAsync(100m);
        await SeedUserAsync("admin", "Admin", "right");

        await ToggleHandler().Handle(new SetAcceptUtangCommand(false, " Admin ", "right"), CancellationToken.None);

        Assert.False((await _ctx.StoreSettings.SingleAsync()).AcceptUtang);
    }

    [Fact]
    public async Task Credit_only_balances_count_as_nothing_owed()
    {
        await SeedOwingSukiAsync(100m, paid: 150m);

        await ToggleHandler().Handle(new SetAcceptUtangCommand(false), CancellationToken.None);

        Assert.False((await _ctx.StoreSettings.SingleAsync()).AcceptUtang);
    }

    [Fact]
    public async Task Outstanding_reports_positive_balances_only()
    {
        await SeedOwingSukiAsync(100m, paid: 30m);
        var credit = new Suki { Name = "Credit", CreatedBy = Guid.NewGuid() };
        _ctx.Sukis.Add(credit);
        _ctx.Payments.Add(new Payment { SukiId = credit.Id, Amount = 20m, CreatedBy = Guid.NewGuid() });
        await _ctx.SaveChangesAsync();

        var result = await new GetUtangOutstandingQueryHandler(_utang)
            .Handle(new GetUtangOutstandingQuery(), CancellationToken.None);

        Assert.Equal(70m, result.TotalOwed);
        Assert.Equal(1, result.OwingCount);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }
}
