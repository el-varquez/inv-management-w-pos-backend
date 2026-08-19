using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Application.Auth.Commands.Login;
using POS.Application.Cashiers.Commands.CreateCashier;
using POS.Application.Cashiers.Commands.DeactivateCashier;
using POS.Application.Cashiers.Commands.ResetCashierPassword;
using POS.Application.Cashiers.Queries.GetCashiers;
using POS.Domain.Exceptions;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;
using POS.Infrastructure.Services;
using Xunit;

namespace POS.Infrastructure.Tests;

public class CashierModuleTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _ctx;
    private readonly UserRepository _users;
    private readonly UnitOfWork _uow;
    private readonly PasswordHasher _hasher = new();

    public CashierModuleTests()
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
    }

    private CreateCashierCommandHandler CreateHandler() =>
        new(_users, _uow, _hasher);

    [Fact]
    public async Task Create_without_password_leaves_hash_null_for_register_claim()
    {
        var id = await CreateHandler().Handle(
            new CreateCashierCommand("Nena", "nena"), CancellationToken.None);

        var cashier = await _ctx.Users.SingleAsync(u => u.Id == id);
        Assert.Null(cashier.PasswordHash);

        var login = new LoginCommandHandler(_users, _hasher);
        var result = await login.Handle(new LoginCommand("nena", "anything"), CancellationToken.None);
        Assert.True(result.PasswordSetupRequired);
        Assert.Equal("Cashier", result.User!.Role);
    }

    [Fact]
    public async Task Create_stamps_role_and_active()
    {
        var id = await CreateHandler().Handle(
            new CreateCashierCommand("Cashier One", "one", "password123"),
            CancellationToken.None);

        var cashier = await _ctx.Users.SingleAsync(u => u.Id == id);
        Assert.Equal("Cashier", cashier.Role);
        Assert.True(cashier.IsActive);
        Assert.Equal("one", cashier.Username);
    }

    [Fact]
    public async Task Create_rejects_duplicate_username()
    {
        await CreateHandler().Handle(
            new CreateCashierCommand("A", "dup", "password123"), CancellationToken.None);

        await Assert.ThrowsAsync<DomainException>(() =>
            CreateHandler().Handle(new CreateCashierCommand("B", "DUP", "password123"), CancellationToken.None));
    }

    [Fact]
    public async Task Reset_password_changes_hash()
    {
        var id = await CreateHandler().Handle(
            new CreateCashierCommand("A", "cashier.a", "password123"), CancellationToken.None);

        var reset = new ResetCashierPasswordCommandHandler(_users, _uow, _hasher);
        await reset.Handle(new ResetCashierPasswordCommand(id, "newpassword123"), CancellationToken.None);

        var cashier = await _ctx.Users.SingleAsync(u => u.Id == id);
        Assert.True(_hasher.Verify("newpassword123", cashier.PasswordHash!));
    }

    [Fact]
    public async Task List_reports_active_count()
    {
        var createHandler = CreateHandler();

        var id = await createHandler.Handle(
            new CreateCashierCommand("A", "cashier.a", "password123"), CancellationToken.None);
        await createHandler.Handle(
            new CreateCashierCommand("B", "cashier.b", "password123"), CancellationToken.None);

        var deactivate = new DeactivateCashierCommandHandler(_users, _uow);
        await deactivate.Handle(new DeactivateCashierCommand(id), CancellationToken.None);

        var query = new GetCashiersQueryHandler(_users);
        var result = await query.Handle(new GetCashiersQuery(), CancellationToken.None);

        Assert.Equal(2, result.Cashiers.Count);
        Assert.Equal(1, result.ActiveCount);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }
}
