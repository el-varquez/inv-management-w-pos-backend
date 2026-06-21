using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Domain.Common;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Tests.Fakes;
using Xunit;

namespace POS.Infrastructure.Tests;

/// <summary>
/// Proves that ITenantScoped entities created during domain-event dispatch
/// (the "second save" in AppDbContext.SaveChangesAsync) are auto-stamped
/// with the current tenant. Before the fix, those entities were persisted
/// with TenantId == Guid.Empty.
/// </summary>
public class DomainEventTenantStampTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public DomainEventTenantStampTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var ctx = new AppDbContext(_options, new FakeCurrentUser { TenantId = null });
        ctx.Database.EnsureCreated();
    }

    /// <summary>
    /// A trivial domain event used only in this test.
    /// </summary>
    private record TestDomainEvent : IDomainEvent
    {
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }

    /// <summary>
    /// A fake IMediator that, on Publish, adds a StockMovement (without setting
    /// TenantId) to the given DbContext — simulating what SaleCompletedEventHandler
    /// does in production. The DbContext reference is set after construction so it
    /// can point to the same context instance that calls SaveChangesAsync.
    /// </summary>
    private sealed class FakeMediator : IMediator
    {
        public AppDbContext? Ctx { get; set; }
        private readonly Guid _itemId;
        private readonly Guid _userId;

        public FakeMediator(Guid itemId, Guid userId)
        {
            _itemId = itemId;
            _userId = userId;
        }

        public Task Publish(object notification, CancellationToken ct = default)
        {
            if (notification is TestDomainEvent)
            {
                // Simulate a domain-event handler adding a tenant-scoped entity
                // WITHOUT setting TenantId (the auto-stamp should handle it).
                Ctx!.StockMovements.Add(new StockMovement
                {
                    ItemId = _itemId,
                    Type = StockMovementType.Sale,
                    Quantity = -1,
                    CreatedBy = _userId
                    // TenantId deliberately NOT set
                });
            }
            return Task.CompletedTask;
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken ct = default)
            where TNotification : INotification
            => Publish((object)notification, ct);

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task Send<TRequest>(TRequest request, CancellationToken ct = default)
            where TRequest : IRequest
            => throw new NotSupportedException();

        public Task<object?> Send(object request, CancellationToken ct = default)
            => throw new NotSupportedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken ct = default)
            => throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken ct = default)
            => throw new NotSupportedException();
    }

    [Fact]
    public async Task Entity_added_during_domain_event_dispatch_gets_tenant_stamped()
    {
        var tenantA = Guid.NewGuid();
        var fakeUser = new FakeCurrentUser { TenantId = tenantA };

        // Seed an Item that the StockMovement FK will reference.
        var category = new Category { Name = "Test Category" };
        var item = new Item
        {
            Name = "Test Item",
            CategoryId = category.Id,
            Category = category,
            CostPrice = 10m,
            SellingPrice = 20m,
            Stock = 100
        };

        await using (var seedCtx = new AppDbContext(_options, fakeUser))
        {
            seedCtx.Categories.Add(category);
            seedCtx.Items.Add(item);
            await seedCtx.SaveChangesAsync();
        }

        // Create the mediator that will inject a StockMovement on Publish.
        var mediator = new FakeMediator(item.Id, fakeUser.Id);

        // Build the test context WITH the mediator wired up.
        await using var testCtx = new AppDbContext(_options, fakeUser, mediator);

        // Point the mediator at the same context so entities land in the same
        // ChangeTracker that SaveChangesAsync will flush.
        mediator.Ctx = testCtx;

        // Add a category with a domain event so the event-dispatch path fires.
        var triggerCategory = new Category { Name = "Trigger" };
        triggerCategory.AddDomainEvent(new TestDomainEvent());
        testCtx.Categories.Add(triggerCategory);

        await testCtx.SaveChangesAsync();

        // Read back the StockMovement bypassing the query filter
        // (null tenant sees nothing through the filter).
        await using var verifyCtx = new AppDbContext(
            _options, new FakeCurrentUser { TenantId = null });
        var movement = await verifyCtx.StockMovements
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(m => m.ItemId == item.Id);

        Assert.NotNull(movement);
        Assert.Equal(tenantA, movement!.TenantId);
    }

    public void Dispose() => _connection.Dispose();
}
