using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Domain.Entities;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Tests.Fakes;
using Xunit;

namespace POS.Infrastructure.Tests;

public class TenantIsolationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public TenantIsolationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var ctx = new AppDbContext(_options, new FakeCurrentUser { TenantId = null });
        ctx.Database.EnsureCreated();
    }

    private AppDbContext ContextFor(Guid? tenantId) =>
        new AppDbContext(_options, new FakeCurrentUser { TenantId = tenantId });

    [Fact]
    public async Task Insert_auto_stamps_current_tenant()
    {
        var tenantA = Guid.NewGuid();
        await using var ctx = ContextFor(tenantA);

        var category = new Category { Name = "Drinks" };
        ctx.Categories.Add(category);
        await ctx.SaveChangesAsync();

        Assert.Equal(tenantA, category.TenantId);
    }

    [Fact]
    public async Task Query_returns_only_current_tenant_rows()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        await using (var ctxA = ContextFor(tenantA))
        {
            ctxA.Categories.Add(new Category { Name = "A-Drinks" });
            await ctxA.SaveChangesAsync();
        }
        await using (var ctxB = ContextFor(tenantB))
        {
            ctxB.Categories.Add(new Category { Name = "B-Snacks" });
            await ctxB.SaveChangesAsync();
        }

        await using var readA = ContextFor(tenantA);
        var namesA = await readA.Categories.Select(c => c.Name).ToListAsync();

        Assert.Equal(new[] { "A-Drinks" }, namesA);
    }

    [Fact]
    public async Task SuperAdmin_with_null_tenant_sees_nothing()
    {
        var tenantA = Guid.NewGuid();
        await using (var ctxA = ContextFor(tenantA))
        {
            ctxA.Categories.Add(new Category { Name = "A-Drinks" });
            await ctxA.SaveChangesAsync();
        }

        await using var superAdmin = ContextFor(null);
        var count = await superAdmin.Categories.CountAsync();

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Insert_without_tenant_context_throws()
    {
        await using var ctx = ContextFor(null);
        ctx.Categories.Add(new Category { Name = "Orphan" });

        await Assert.ThrowsAsync<InvalidOperationException>(() => ctx.SaveChangesAsync());
    }

    public void Dispose() => _connection.Dispose();
}
