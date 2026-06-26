using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Application.Auth.Commands.Register;
using POS.Application.Common.Interfaces;
using POS.Domain.Entities;
using POS.Domain.Exceptions;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;
using POS.Infrastructure.Services;
using POS.Infrastructure.Tests.Fakes;
using Xunit;

namespace POS.Infrastructure.Tests;

public class RegistrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _ctx;
    private readonly RegisterCommandHandler _handler;

    private sealed class StubJwtService : IJwtService
    {
        public string GenerateToken(User user) => $"token-for-{user.Email}";
    }

    public RegistrationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _ctx = new AppDbContext(options, new FakeCurrentUser { TenantId = null });
        _ctx.Database.EnsureCreated();

        var uow = new UnitOfWork(_ctx);
        _handler = new RegisterCommandHandler(
            new TenantRepository(_ctx),
            new UserRepository(_ctx),
            uow,
            new StubJwtService(),
            new PasswordHasher());
    }

    [Fact]
    public async Task Register_creates_tenant_and_admin_and_returns_token()
    {
        var result = await _handler.Handle(
            new RegisterCommand("Aling Nena Store", "Nena Cruz", "nena@store.ph", "password123"),
            CancellationToken.None);

        Assert.Equal("Admin", result.Role);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));

        var tenant = Assert.Single(await _ctx.Tenants.ToListAsync());
        Assert.Equal("Aling Nena Store", tenant.Name);
        Assert.Equal(5, tenant.CashierCap);

        var user = await _ctx.Users.SingleAsync();
        Assert.Equal("Admin", user.Role);
        Assert.Equal(tenant.Id, user.TenantId);
        Assert.Equal("nena@store.ph", user.Email);
    }

    [Fact]
    public async Task Register_rejects_duplicate_email()
    {
        await _handler.Handle(
            new RegisterCommand("Store One", "Owner One", "dup@store.ph", "password123"),
            CancellationToken.None);

        await Assert.ThrowsAsync<DomainException>(() =>
            _handler.Handle(
                new RegisterCommand("Store Two", "Owner Two", "DUP@store.ph", "password123"),
                CancellationToken.None));
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }
}
