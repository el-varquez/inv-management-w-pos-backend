using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Application.Platform.Commands.CreateTenant;
using POS.Application.Platform.Commands.DeactivateTenantUser;
using POS.Application.Platform.Commands.EditTenantUser;
using POS.Application.Platform.Commands.ReactivateTenantUser;
using POS.Domain.Entities;
using POS.Domain.Exceptions;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;
using POS.Infrastructure.Services;
using POS.Infrastructure.Tests.Fakes;
using Xunit;

namespace POS.Infrastructure.Tests;

public class PlatformUserModuleTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _ctx;
    private readonly UserRepository _users;
    private readonly TenantRepository _tenants;
    private readonly UnitOfWork _uow;
    private readonly PasswordHasher _hasher = new();

    public PlatformUserModuleTests()
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

    private async Task<Guid> SeedTenantAsync(string email = "owner@store.ph", int cap = 5)
        => await new CreateTenantCommandHandler(_tenants, _users, _uow, _hasher).Handle(
            new CreateTenantCommand("Store", "Owner", email, "password123", cap),
            CancellationToken.None);

    private async Task<User> AddCashierAsync(Guid tenantId, string email, bool active = true)
    {
        var cashier = new User
        {
            Name = "Cashier",
            Email = email,
            PasswordHash = _hasher.Hash("password123"),
            Role = "Cashier",
            IsActive = active,
            TenantId = tenantId
        };
        await _users.AddAsync(cashier);
        await _uow.SaveChangesAsync();
        return cashier;
    }

    private async Task<User> AdminOf(Guid tenantId)
        => await _ctx.Users.SingleAsync(u => u.TenantId == tenantId && u.Role == "Admin");

    private EditTenantUserCommandHandler EditHandler() => new(_users, _uow, _hasher);
    private DeactivateTenantUserCommandHandler DeactivateHandler() => new(_users, _uow);
    private ReactivateTenantUserCommandHandler ReactivateHandler() => new(_users, _tenants, _uow);

    [Fact]
    public async Task Edit_changes_name_and_email()
    {
        var tenantId = await SeedTenantAsync();
        var admin = await AdminOf(tenantId);

        await EditHandler().Handle(
            new EditTenantUserCommand(tenantId, admin.Id, "Renamed Owner", "renamed@store.ph", null),
            CancellationToken.None);

        var updated = await _ctx.Users.SingleAsync(u => u.Id == admin.Id);
        Assert.Equal("Renamed Owner", updated.Name);
        Assert.Equal("renamed@store.ph", updated.Email);
    }

    [Fact]
    public async Task Edit_rejects_duplicate_email()
    {
        var tenantId = await SeedTenantAsync("owner@store.ph");
        var cashier = await AddCashierAsync(tenantId, "cashier@store.ph");

        await Assert.ThrowsAsync<DomainException>(() =>
            EditHandler().Handle(
                new EditTenantUserCommand(tenantId, cashier.Id, "Cashier", "OWNER@store.ph", null),
                CancellationToken.None));
    }

    [Fact]
    public async Task Edit_with_blank_password_leaves_hash_unchanged()
    {
        var tenantId = await SeedTenantAsync();
        var admin = await AdminOf(tenantId);
        var originalHash = admin.PasswordHash;

        await EditHandler().Handle(
            new EditTenantUserCommand(tenantId, admin.Id, "Owner", "owner@store.ph", null),
            CancellationToken.None);

        var updated = await _ctx.Users.SingleAsync(u => u.Id == admin.Id);
        Assert.Equal(originalHash, updated.PasswordHash);
        Assert.True(_hasher.Verify("password123", updated.PasswordHash));
    }

    [Fact]
    public async Task Edit_with_password_rehashes()
    {
        var tenantId = await SeedTenantAsync();
        var admin = await AdminOf(tenantId);

        await EditHandler().Handle(
            new EditTenantUserCommand(tenantId, admin.Id, "Owner", "owner@store.ph", "newpassword123"),
            CancellationToken.None);

        var updated = await _ctx.Users.SingleAsync(u => u.Id == admin.Id);
        Assert.True(_hasher.Verify("newpassword123", updated.PasswordHash));
    }

    [Fact]
    public async Task Deactivate_rejects_admin_target()
    {
        var tenantId = await SeedTenantAsync();
        var admin = await AdminOf(tenantId);

        await Assert.ThrowsAsync<DomainException>(() =>
            DeactivateHandler().Handle(
                new DeactivateTenantUserCommand(tenantId, admin.Id),
                CancellationToken.None));

        var stillActive = await _ctx.Users.SingleAsync(u => u.Id == admin.Id);
        Assert.True(stillActive.IsActive);
    }

    [Fact]
    public async Task Deactivate_cashier_frees_a_slot()
    {
        var tenantId = await SeedTenantAsync(cap: 5);
        var cashier = await AddCashierAsync(tenantId, "cashier@store.ph");

        await DeactivateHandler().Handle(
            new DeactivateTenantUserCommand(tenantId, cashier.Id),
            CancellationToken.None);

        Assert.Equal(0, await _users.CountActiveCashiersAsync(tenantId));
        var updated = await _ctx.Users.SingleAsync(u => u.Id == cashier.Id);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task Reactivate_rechecks_cap()
    {
        var tenantId = await SeedTenantAsync(cap: 1);
        var inactive = await AddCashierAsync(tenantId, "a@store.ph", active: false);
        await AddCashierAsync(tenantId, "b@store.ph", active: true);

        await Assert.ThrowsAsync<DomainException>(() =>
            ReactivateHandler().Handle(
                new ReactivateTenantUserCommand(tenantId, inactive.Id),
                CancellationToken.None));

        var stillInactive = await _ctx.Users.SingleAsync(u => u.Id == inactive.Id);
        Assert.False(stillInactive.IsActive);
    }

    [Fact]
    public async Task Reactivate_succeeds_below_cap()
    {
        var tenantId = await SeedTenantAsync(cap: 2);
        var inactive = await AddCashierAsync(tenantId, "a@store.ph", active: false);

        await ReactivateHandler().Handle(
            new ReactivateTenantUserCommand(tenantId, inactive.Id),
            CancellationToken.None);

        var updated = await _ctx.Users.SingleAsync(u => u.Id == inactive.Id);
        Assert.True(updated.IsActive);
    }

    [Fact]
    public async Task Edit_user_not_in_tenant_returns_NotFound()
    {
        var tenantA = await SeedTenantAsync("a@store.ph");
        var tenantB = await SeedTenantAsync("b@store.ph");
        var adminA = await AdminOf(tenantA);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            EditHandler().Handle(
                new EditTenantUserCommand(tenantB, adminA.Id, "X", "x@store.ph", null),
                CancellationToken.None));
    }

    [Fact]
    public async Task Deactivate_user_not_in_tenant_returns_NotFound()
    {
        var tenantA = await SeedTenantAsync("a@store.ph");
        var tenantB = await SeedTenantAsync("b@store.ph");
        var cashierA = await AddCashierAsync(tenantA, "ca@store.ph");

        await Assert.ThrowsAsync<NotFoundException>(() =>
            DeactivateHandler().Handle(
                new DeactivateTenantUserCommand(tenantB, cashierA.Id),
                CancellationToken.None));
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }
}
