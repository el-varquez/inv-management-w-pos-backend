
using Microsoft.EntityFrameworkCore;
using POS.Domain.Entities;
using MediatR;
using POS.Domain.Common;
using POS.Application.Common.Interfaces;

namespace POS.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    private readonly IMediator? _mediator;
    private readonly ICurrentUser? _currentUser;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        ICurrentUser currentUser,
        IMediator? mediator = null)
        : base(options)
    {
        _currentUser = currentUser;
        _mediator = mediator;
    }


    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<TransactionItem> TransactionItems => Set<TransactionItem>();
    public DbSet<CompositeItem> CompositeItems => Set<CompositeItem>();
    public DbSet<InventoryCount> InventoryCounts => Set<InventoryCount>();
    public DbSet<InventoryCountLine> InventoryCountLines => Set<InventoryCountLine>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<User>().HasIndex(u => u.Email).IsUnique();

        builder.Entity<Item>().Property(i => i.CostPrice).HasPrecision(18, 2);
        builder.Entity<Item>().Property(i => i.SellingPrice).HasPrecision(18, 2);
        builder.Entity<Item>()
            .HasOne(i => i.Category)
            .WithMany(c => c.Items)
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<StockMovement>().Property(s => s.CostPerUnit).HasPrecision(18, 2);
        builder.Entity<StockMovement>()
            .HasOne(s => s.Item)
            .WithMany(s => s.StockMovements)
            .HasForeignKey(s => s.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Transaction>().Property(t => t.Subtotal).HasPrecision(18, 2);
        builder.Entity<Transaction>().Property(t => t.DiscountAmount).HasPrecision(18, 2);
        builder.Entity<Transaction>().Property(t => t.Total).HasPrecision(18, 2);
        builder.Entity<Transaction>().Property(t => t.AmountTendered).HasPrecision(18, 2);
        builder.Entity<Transaction>().Property(t => t.Change).HasPrecision(18, 2);
        builder.Entity<Transaction>().HasIndex(t => t.ReceiptNumber).IsUnique();

        builder.Entity<TransactionItem>().Property(ti => ti.UnitPrice).HasPrecision(18, 2);
        builder.Entity<TransactionItem>().Property(ti => ti.CostPrice).HasPrecision(18, 2);
        builder.Entity<TransactionItem>().Property(ti => ti.Discount).HasPrecision(18, 2);
        builder.Entity<TransactionItem>().Property(ti => ti.Total).HasPrecision(18, 2);
        builder.Entity<TransactionItem>()
            .HasOne(ti => ti.Transaction)
            .WithMany(ti => ti.Items)
            .HasForeignKey(ti => ti.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<TransactionItem>()
            .HasOne(ti => ti.Item)
            .WithMany(i => i.TransactionItems)
            .HasForeignKey(ti => ti.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<CompositeItem>()
            .HasOne(c => c.ParentItem)
            .WithMany(i => i.Components)
            .HasForeignKey(c => c.ParentItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CompositeItem>()
            .HasOne(c => c.ComponentItem)
            .WithMany(i => i.UsedInItems)
            .HasForeignKey(c => c.ComponentItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<CompositeItem>().Property(c => c.Quantity).HasPrecision(18, 3);

        builder.Entity<InventoryCount>().HasIndex(ic => ic.Reference).IsUnique();

        builder.Entity<InventoryCountLine>()
            .HasOne(l => l.InventoryCount)
            .WithMany(ic => ic.Lines)
            .HasForeignKey(l => l.InventoryCountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<InventoryCountLine>()
            .HasOne(l => l.Item)
            .WithMany()
            .HasForeignKey(l => l.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Category>().HasQueryFilter(e => e.TenantId == _currentUser!.TenantId);
        builder.Entity<Item>().HasQueryFilter(e => e.TenantId == _currentUser!.TenantId);
        builder.Entity<StockMovement>().HasQueryFilter(e => e.TenantId == _currentUser!.TenantId);
        builder.Entity<Transaction>().HasQueryFilter(e => e.TenantId == _currentUser!.TenantId);
        builder.Entity<TransactionItem>().HasQueryFilter(e => e.TenantId == _currentUser!.TenantId);
        builder.Entity<CompositeItem>().HasQueryFilter(e => e.TenantId == _currentUser!.TenantId);
        builder.Entity<InventoryCount>().HasQueryFilter(e => e.TenantId == _currentUser!.TenantId);
        builder.Entity<InventoryCountLine>().HasQueryFilter(e => e.TenantId == _currentUser!.TenantId);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        StampTenantScopedEntities();

        var entitiesWithEvents = ChangeTracker.Entries<BaseEntity>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Any())
            .ToList();

        var result = await base.SaveChangesAsync(ct);

        if (_mediator is not null)
        {
            foreach (var entity in entitiesWithEvents)
            {
                var events = entity.DomainEvents.ToList();
                entity.ClearDomainEvents();
                foreach (var domainEvent in events)
                    await _mediator.Publish(domainEvent, ct);
            }

            StampTenantScopedEntities();
            await base.SaveChangesAsync(ct);
        }

        return result;
    }

    private void StampTenantScopedEntities()
    {
        foreach (var entry in ChangeTracker.Entries<ITenantScoped>()
                     .Where(e => e.State == EntityState.Added))
        {
            if (_currentUser?.TenantId is not Guid tenantId)
                throw new InvalidOperationException(
                    "Cannot persist a tenant-scoped entity without a tenant context.");
            entry.Entity.TenantId = tenantId;
        }
    }
}