using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Application.Auth.Commands.Login;
using POS.Application.Auth.Commands.SetupPassword;
using POS.Domain.Entities;
using POS.Domain.Exceptions;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;
using POS.Infrastructure.Services;
using POS.Infrastructure.Tests.Fakes;
using Xunit;

namespace POS.Infrastructure.Tests;

public class LoginGateTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _ctx;
    private readonly UserRepository _users;
    private readonly UnitOfWork _uow;
    private readonly PasswordHasher _hasher = new();
    private readonly LoginCommandHandler _login;
    private readonly SetupPasswordCommandHandler _setup;

    public LoginGateTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _ctx = new AppDbContext(options);
        _ctx.Database.EnsureCreated();

        _users = new UserRepository(_ctx);
        _uow = new UnitOfWork(_ctx);
        _login = new LoginCommandHandler(_users, _hasher);
        _setup = new SetupPasswordCommandHandler(_users, _hasher, _uow);
    }

    private async Task<User> SeedUserAsync(
        string email, string? password = "password123", string role = "Admin", bool isActive = true)
    {
        var user = new User
        {
            Name = "User",
            Email = email,
            PasswordHash = password is null ? null : _hasher.Hash(password),
            Role = role,
            IsActive = isActive
        };
        await _users.AddAsync(user);
        await _uow.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task Login_returns_user_for_valid_credentials()
    {
        await SeedUserAsync("admin@store.ph");

        var result = await _login.Handle(
            new LoginCommand("admin@store.ph", "password123"), CancellationToken.None);

        Assert.False(result.PasswordSetupRequired);
        Assert.NotNull(result.User);
        Assert.Equal("admin@store.ph", result.User!.Email);
    }

    [Fact]
    public async Task Login_rejects_wrong_password()
    {
        await SeedUserAsync("admin@store.ph");

        await Assert.ThrowsAsync<DomainException>(() =>
            _login.Handle(new LoginCommand("admin@store.ph", "wrong"), CancellationToken.None));
    }

    [Fact]
    public async Task Login_rejects_inactive_account()
    {
        await SeedUserAsync("gone@store.ph", isActive: false);

        await Assert.ThrowsAsync<DomainException>(() =>
            _login.Handle(new LoginCommand("gone@store.ph", "password123"), CancellationToken.None));
    }

    [Fact]
    public async Task Login_signals_password_setup_when_no_password_set()
    {
        await SeedUserAsync("new@store.ph", password: null);

        var result = await _login.Handle(
            new LoginCommand("new@store.ph", "anything"), CancellationToken.None);

        Assert.True(result.PasswordSetupRequired);
        Assert.Null(result.User);
    }

    [Fact]
    public async Task SetupPassword_sets_hash_and_signs_in()
    {
        var seeded = await SeedUserAsync("new@store.ph", password: null);

        var result = await _setup.Handle(
            new SetupPasswordCommand("new@store.ph", "newpassword123"), CancellationToken.None);

        Assert.False(result.PasswordSetupRequired);
        Assert.Equal(seeded.Id, result.User!.Id);

        var persisted = await _ctx.Users.SingleAsync(u => u.Id == seeded.Id);
        Assert.True(_hasher.Verify("newpassword123", persisted.PasswordHash!));
    }

    [Fact]
    public async Task SetupPassword_rejected_when_password_already_set()
    {
        await SeedUserAsync("admin@store.ph");

        await Assert.ThrowsAsync<DomainException>(() =>
            _setup.Handle(new SetupPasswordCommand("admin@store.ph", "newpassword123"), CancellationToken.None));
    }

    [Fact]
    public void SetupPassword_validator_rejects_short_password()
    {
        var result = new SetupPasswordCommandValidator()
            .Validate(new SetupPasswordCommand("new@store.ph", "short"));
        Assert.False(result.IsValid);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }
}
