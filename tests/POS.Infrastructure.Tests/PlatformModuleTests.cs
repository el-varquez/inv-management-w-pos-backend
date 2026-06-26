using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Application.Platform.Commands.CreateTenant;
using POS.Application.Platform.Commands.ReactivateTenant;
using POS.Application.Platform.Commands.SetCashierCap;
using POS.Application.Platform.Commands.SuspendTenant;
using POS.Application.Platform.Queries.GetTenant;
using POS.Application.Platform.Queries.GetTenants;
using POS.Domain.Entities;
using POS.Domain.Exceptions;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;
using POS.Infrastructure.Services;
using POS.Infrastructure.Tests.Fakes;
using Xunit;

namespace POS.Infrastructure.Tests;

public class PlatformModuleTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _ctx;
    private readonly UserRepository _users;
    private readonly TenantRepository _tenants;
    private readonly UnitOfWork _uow;
    private readonly PasswordHasher _hasher = new();

    public PlatformModuleTests()
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
    }

    private CreateTenantCommandHandler CreateHandler() =>
        new(_tenants, _users, _uow, _hasher);

    [Fact]
    public async Task CreateTenant_creates_tenant_and_admin_atomically()
    {
        var id = await CreateHandler().Handle(
            new CreateTenantCommand("Aling Nena Store", "Nena Cruz", "nena@store.ph", "password123", null),
            CancellationToken.None);

        var tenant = await _ctx.Tenants.SingleAsync(t => t.Id == id);
        Assert.Equal("Aling Nena Store", tenant.Name);
        Assert.True(tenant.IsActive);
        Assert.Equal(5, tenant.CashierCap);

        var admin = await _ctx.Users.SingleAsync(u => u.TenantId == id);
        Assert.Equal("Admin", admin.Role);
        Assert.Equal("nena@store.ph", admin.Email);
    }

    [Fact]
    public async Task CreateTenant_honors_supplied_cap()
    {
        var id = await CreateHandler().Handle(
            new CreateTenantCommand("Store", "Owner", "owner@store.ph", "password123", 12),
            CancellationToken.None);

        var tenant = await _ctx.Tenants.SingleAsync(t => t.Id == id);
        Assert.Equal(12, tenant.CashierCap);
    }

    [Fact]
    public async Task CreateTenant_rejects_duplicate_email()
    {
        await CreateHandler().Handle(
            new CreateTenantCommand("Store One", "Owner One", "dup@store.ph", "password123", null),
            CancellationToken.None);

        await Assert.ThrowsAsync<DomainException>(() =>
            CreateHandler().Handle(
                new CreateTenantCommand("Store Two", "Owner Two", "DUP@store.ph", "password123", null),
                CancellationToken.None));
    }

    [Fact]
    public async Task Suspend_then_reactivate_toggles_isactive()
    {
        var id = await CreateHandler().Handle(
            new CreateTenantCommand("Store", "Owner", "owner@store.ph", "password123", null),
            CancellationToken.None);

        await new SuspendTenantCommandHandler(_tenants, _uow)
            .Handle(new SuspendTenantCommand(id), CancellationToken.None);
        Assert.False((await _ctx.Tenants.SingleAsync(t => t.Id == id)).IsActive);

        await new ReactivateTenantCommandHandler(_tenants, _uow)
            .Handle(new ReactivateTenantCommand(id), CancellationToken.None);
        Assert.True((await _ctx.Tenants.SingleAsync(t => t.Id == id)).IsActive);
    }

    [Fact]
    public async Task SetCashierCap_allows_lowering_below_active_count()
    {
        var id = await CreateHandler().Handle(
            new CreateTenantCommand("Store", "Owner", "owner@store.ph", "password123", 5),
            CancellationToken.None);

        for (var i = 0; i < 3; i++)
        {
            await _users.AddAsync(new User
            {
                Name = $"Cashier {i}",
                Email = $"c{i}@store.ph",
                PasswordHash = _hasher.Hash("password123"),
                Role = "Cashier",
                IsActive = true,
                TenantId = id
            });
        }
        await _uow.SaveChangesAsync();

        await new SetCashierCapCommandHandler(_tenants, _uow)
            .Handle(new SetCashierCapCommand(id, 1), CancellationToken.None);

        var tenant = await _ctx.Tenants.SingleAsync(t => t.Id == id);
        Assert.Equal(1, tenant.CashierCap);
        Assert.Equal(3, await _ctx.Users.CountAsync(u => u.TenantId == id && u.Role == "Cashier" && u.IsActive));
    }

    [Fact]
    public async Task GetTenants_returns_all_with_user_and_cashier_counts()
    {
        var idA = await CreateHandler().Handle(
            new CreateTenantCommand("Store A", "Owner A", "a@store.ph", "password123", 5),
            CancellationToken.None);
        await CreateHandler().Handle(
            new CreateTenantCommand("Store B", "Owner B", "b@store.ph", "password123", 5),
            CancellationToken.None);

        await _users.AddAsync(new User
        {
            Name = "Active Cashier",
            Email = "ac@store.ph",
            PasswordHash = _hasher.Hash("password123"),
            Role = "Cashier",
            IsActive = true,
            TenantId = idA
        });
        await _users.AddAsync(new User
        {
            Name = "Inactive Cashier",
            Email = "ic@store.ph",
            PasswordHash = _hasher.Hash("password123"),
            Role = "Cashier",
            IsActive = false,
            TenantId = idA
        });
        await _uow.SaveChangesAsync();

        var result = await new GetTenantsQueryHandler(_tenants, _users)
            .Handle(new GetTenantsQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
        var a = result.Single(t => t.Id == idA);
        Assert.Equal(3, a.UserCount);
        Assert.Equal(1, a.ActiveCashierCount);
    }

    [Fact]
    public async Task GetTenant_returns_detail_with_users()
    {
        var id = await CreateHandler().Handle(
            new CreateTenantCommand("Store", "Owner", "owner@store.ph", "password123", 5),
            CancellationToken.None);

        var detail = await new GetTenantQueryHandler(_tenants, _users)
            .Handle(new GetTenantQuery(id), CancellationToken.None);

        Assert.Equal("Store", detail.Name);
        Assert.Single(detail.Users);
        Assert.Equal("Admin", detail.Users[0].Role);
    }

    [Fact]
    public async Task GetTenant_throws_when_missing()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new GetTenantQueryHandler(_tenants, _users)
                .Handle(new GetTenantQuery(Guid.NewGuid()), CancellationToken.None));
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }
}
