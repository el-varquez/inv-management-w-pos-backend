using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Application.Auth.Commands.Login;
using POS.Application.Common.Interfaces;
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
    private readonly TenantRepository _tenants;
    private readonly UnitOfWork _uow;
    private readonly PasswordHasher _hasher = new();
    private readonly LoginCommandHandler _handler;

    private sealed class StubJwtService : IJwtService
    {
        public string GenerateToken(User user) => $"token-for-{user.Email}";
    }

    public LoginGateTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _ctx = new AppDbContext(options, new FakeCurrentUser { TenantId = null });
        _ctx.Database.EnsureCreated();

        _users = new UserRepository(_ctx);
        _tenants = new TenantRepository(_ctx);
        _uow = new UnitOfWork(_ctx);
        _handler = new LoginCommandHandler(_users, new StubJwtService(), _hasher, _tenants);
    }

    private async Task<Tenant> SeedTenantAsync(bool isActive)
    {
        var tenant = new Tenant { Name = "Store", IsActive = isActive };
        await _tenants.AddAsync(tenant);
        await _uow.SaveChangesAsync();
        return tenant;
    }

    private async Task<User> SeedUserAsync(string email, Guid? tenantId, string role = "Admin")
    {
        var user = new User
        {
            Name = "User",
            Email = email,
            PasswordHash = _hasher.Hash("password123"),
            Role = role,
            IsActive = true,
            TenantId = tenantId
        };
        await _users.AddAsync(user);
        await _uow.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task Login_blocked_when_tenant_suspended()
    {
        var tenant = await SeedTenantAsync(isActive: false);
        await SeedUserAsync("admin@store.ph", tenant.Id);

        await Assert.ThrowsAsync<DomainException>(() =>
            _handler.Handle(new LoginCommand("admin@store.ph", "password123"), CancellationToken.None));
    }

    [Fact]
    public async Task Login_allowed_when_tenant_active()
    {
        var tenant = await SeedTenantAsync(isActive: true);
        await SeedUserAsync("admin@store.ph", tenant.Id);

        var result = await _handler.Handle(
            new LoginCommand("admin@store.ph", "password123"), CancellationToken.None);

        Assert.Equal("admin@store.ph", result.Email);
    }

    [Fact]
    public async Task Login_allowed_for_superadmin_without_tenant()
    {
        await SeedUserAsync("super@platform.ph", tenantId: null, role: "SuperAdmin");

        var result = await _handler.Handle(
            new LoginCommand("super@platform.ph", "password123"), CancellationToken.None);

        Assert.Equal("SuperAdmin", result.Role);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }
}
