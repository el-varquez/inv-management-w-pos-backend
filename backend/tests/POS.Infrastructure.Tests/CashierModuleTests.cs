using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Application.Cashiers.Commands.CreateCashier;
using POS.Application.Cashiers.Commands.DeactivateCashier;
using POS.Application.Cashiers.Commands.ReactivateCashier;
using POS.Application.Cashiers.Commands.ResetCashierPassword;
using POS.Application.Cashiers.Queries.GetCashiers;
using POS.Domain.Entities;
using POS.Domain.Exceptions;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;
using POS.Infrastructure.Services;
using POS.Infrastructure.Tests.Fakes;
using Xunit;

namespace POS.Infrastructure.Tests;

public class CashierModuleTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _ctx;
    private readonly UserRepository _users;
    private readonly TenantRepository _tenants;
    private readonly UnitOfWork _uow;
    private readonly PasswordHasher _hasher = new();

    public CashierModuleTests()
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

    private async Task<Tenant> SeedTenantAsync(int cap = 5)
    {
        var tenant = new Tenant { Name = "Store", CashierCap = cap };
        await _tenants.AddAsync(tenant);
        await _uow.SaveChangesAsync();
        return tenant;
    }

    private FakeCurrentUser AdminOf(Guid tenantId) =>
        new() { Role = "Admin", TenantId = tenantId };

    private CreateCashierCommandHandler CreateHandler(Guid tenantId) =>
        new(_users, _tenants, _uow, _hasher, AdminOf(tenantId));

    [Fact]
    public async Task Create_succeeds_below_cap_and_stamps_tenant_and_role()
    {
        var tenant = await SeedTenantAsync(cap: 5);
        var handler = CreateHandler(tenant.Id);

        var id = await handler.Handle(
            new CreateCashierCommand("Cashier One", "one@store.ph", "password123"),
            CancellationToken.None);

        var cashier = await _ctx.Users.SingleAsync(u => u.Id == id);
        Assert.Equal("Cashier", cashier.Role);
        Assert.Equal(tenant.Id, cashier.TenantId);
        Assert.True(cashier.IsActive);
        Assert.Equal("one@store.ph", cashier.Email);
    }

    [Fact]
    public async Task Create_blocked_at_cap()
    {
        var tenant = await SeedTenantAsync(cap: 2);
        var handler = CreateHandler(tenant.Id);

        await handler.Handle(new CreateCashierCommand("A", "a@store.ph", "password123"), CancellationToken.None);
        await handler.Handle(new CreateCashierCommand("B", "b@store.ph", "password123"), CancellationToken.None);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new CreateCashierCommand("C", "c@store.ph", "password123"), CancellationToken.None));
    }

    [Fact]
    public async Task Create_rejects_duplicate_email()
    {
        var tenant = await SeedTenantAsync();
        var handler = CreateHandler(tenant.Id);

        await handler.Handle(new CreateCashierCommand("A", "dup@store.ph", "password123"), CancellationToken.None);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new CreateCashierCommand("B", "DUP@store.ph", "password123"), CancellationToken.None));
    }

    [Fact]
    public async Task Deactivate_frees_a_slot()
    {
        var tenant = await SeedTenantAsync(cap: 1);
        var createHandler = CreateHandler(tenant.Id);

        var id = await createHandler.Handle(
            new CreateCashierCommand("A", "a@store.ph", "password123"), CancellationToken.None);

        await Assert.ThrowsAsync<DomainException>(() =>
            createHandler.Handle(new CreateCashierCommand("B", "b@store.ph", "password123"), CancellationToken.None));

        var deactivate = new DeactivateCashierCommandHandler(_users, _uow, AdminOf(tenant.Id));
        await deactivate.Handle(new DeactivateCashierCommand(id), CancellationToken.None);

        var newId = await createHandler.Handle(
            new CreateCashierCommand("B", "b@store.ph", "password123"), CancellationToken.None);
        Assert.NotEqual(Guid.Empty, newId);
    }

    [Fact]
    public async Task Reactivate_rechecks_cap()
    {
        var tenant = await SeedTenantAsync(cap: 1);
        var createHandler = CreateHandler(tenant.Id);

        var firstId = await createHandler.Handle(
            new CreateCashierCommand("A", "a@store.ph", "password123"), CancellationToken.None);

        var deactivate = new DeactivateCashierCommandHandler(_users, _uow, AdminOf(tenant.Id));
        await deactivate.Handle(new DeactivateCashierCommand(firstId), CancellationToken.None);

        await createHandler.Handle(
            new CreateCashierCommand("B", "b@store.ph", "password123"), CancellationToken.None);

        var reactivate = new ReactivateCashierCommandHandler(_users, _tenants, _uow, AdminOf(tenant.Id));
        await Assert.ThrowsAsync<DomainException>(() =>
            reactivate.Handle(new ReactivateCashierCommand(firstId), CancellationToken.None));
    }

    [Fact]
    public async Task AdminA_cannot_list_AdminB_cashiers()
    {
        var tenantA = await SeedTenantAsync();
        var tenantB = await SeedTenantAsync();

        await CreateHandler(tenantA.Id).Handle(
            new CreateCashierCommand("A-Cashier", "a@store.ph", "password123"), CancellationToken.None);

        var query = new GetCashiersQueryHandler(_users, _tenants, AdminOf(tenantB.Id));
        var result = await query.Handle(new GetCashiersQuery(), CancellationToken.None);

        Assert.Empty(result.Cashiers);
        Assert.Equal(0, result.ActiveCount);
    }

    [Fact]
    public async Task AdminA_cannot_deactivate_AdminB_cashier()
    {
        var tenantA = await SeedTenantAsync();
        var tenantB = await SeedTenantAsync();

        var id = await CreateHandler(tenantA.Id).Handle(
            new CreateCashierCommand("A-Cashier", "a@store.ph", "password123"), CancellationToken.None);

        var deactivate = new DeactivateCashierCommandHandler(_users, _uow, AdminOf(tenantB.Id));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            deactivate.Handle(new DeactivateCashierCommand(id), CancellationToken.None));

        var stillActive = await _ctx.Users.SingleAsync(u => u.Id == id);
        Assert.True(stillActive.IsActive);
    }

    [Fact]
    public async Task AdminA_cannot_reset_AdminB_cashier_password()
    {
        var tenantA = await SeedTenantAsync();
        var tenantB = await SeedTenantAsync();

        var id = await CreateHandler(tenantA.Id).Handle(
            new CreateCashierCommand("A-Cashier", "a@store.ph", "password123"), CancellationToken.None);

        var reset = new ResetCashierPasswordCommandHandler(_users, _uow, _hasher, AdminOf(tenantB.Id));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            reset.Handle(new ResetCashierPasswordCommand(id, "newpassword123"), CancellationToken.None));
    }

    [Fact]
    public async Task Reset_password_changes_hash()
    {
        var tenant = await SeedTenantAsync();
        var id = await CreateHandler(tenant.Id).Handle(
            new CreateCashierCommand("A", "a@store.ph", "password123"), CancellationToken.None);

        var reset = new ResetCashierPasswordCommandHandler(_users, _uow, _hasher, AdminOf(tenant.Id));
        await reset.Handle(new ResetCashierPasswordCommand(id, "newpassword123"), CancellationToken.None);

        var cashier = await _ctx.Users.SingleAsync(u => u.Id == id);
        Assert.True(_hasher.Verify("newpassword123", cashier.PasswordHash));
    }

    [Fact]
    public async Task List_reports_cap_and_active_count()
    {
        var tenant = await SeedTenantAsync(cap: 5);
        var createHandler = CreateHandler(tenant.Id);

        var id = await createHandler.Handle(
            new CreateCashierCommand("A", "a@store.ph", "password123"), CancellationToken.None);
        await createHandler.Handle(
            new CreateCashierCommand("B", "b@store.ph", "password123"), CancellationToken.None);

        var deactivate = new DeactivateCashierCommandHandler(_users, _uow, AdminOf(tenant.Id));
        await deactivate.Handle(new DeactivateCashierCommand(id), CancellationToken.None);

        var query = new GetCashiersQueryHandler(_users, _tenants, AdminOf(tenant.Id));
        var result = await query.Handle(new GetCashiersQuery(), CancellationToken.None);

        Assert.Equal(2, result.Cashiers.Count);
        Assert.Equal(1, result.ActiveCount);
        Assert.Equal(5, result.CashierCap);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }
}
