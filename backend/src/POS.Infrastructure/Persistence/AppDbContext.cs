
using Microsoft.EntityFrameworkCore;
using POS.Domain.Entities;

namespace POS.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<TransactionItem> TransactionItems => Set<TransactionItem>();

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
        builder.Entity<Transaction>().Property(t => t.Change).HasPrecision(18, 20);
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
    }
}