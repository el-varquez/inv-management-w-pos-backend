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

    private record TestDomainEvent : IDomainEvent
    {
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }

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
                Ctx!.StockMovements.Add(new StockMovement
                {
                    ItemId = _itemId,
                    Type = StockMovementType.Sale,
                    Quantity = -1,
                    CreatedBy = _userId
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

        var mediator = new FakeMediator(item.Id, fakeUser.Id);

        await using var testCtx = new AppDbContext(_options, fakeUser, mediator);

        mediator.Ctx = testCtx;

        var triggerCategory = new Category { Name = "Trigger" };
        triggerCategory.AddDomainEvent(new TestDomainEvent());
        testCtx.Categories.Add(triggerCategory);

        await testCtx.SaveChangesAsync();

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
