using Microsoft.EntityFrameworkCore;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Domain.Interfaces;

namespace POS.Infrastructure.Persistence.Repositories;

public class StockMovementRepository : IStockMovementRepository
{
    private readonly AppDbContext _context;
    public StockMovementRepository(AppDbContext context) => _context = context;

    public async Task<IList<StockMovement>> GetByItemIdAsync(Guid itemId, CancellationToken ct = default)
        => await _context.StockMovements
        .Include(s => s.Item)
        .Where(s => s.ItemId == itemId)
        .OrderByDescending(s => s.CreatedAt)
        .ToListAsync(ct);

    public async Task<IList<StockMovement>> GetAllAsync(
        DateTime? from,
        DateTime? to,
        StockMovementType? type,
        CancellationToken ct = default
    )
    {
        var query = _context.StockMovements
        .Include(s => s.Item)
        .ThenInclude(i => i.Category)
        .AsQueryable();

        if (from.HasValue) query = query.Where(s => s.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(s => s.CreatedAt <= to.Value);
        if (type.HasValue) query = query.Where(s => s.Type == type.Value);

        return await query.OrderByDescending(s => s.CreatedAt).ToListAsync(ct);
    }

    public async Task<(IList<StockMovement> Items, int Total)> GetPagedAsync(
        DateTime? from,
        DateTime? to,
        StockMovementType? type,
        int page,
        int pageSize,
        CancellationToken ct = default
    )
    {
        var query = _context.StockMovements
        .Include(s => s.Item)
        .ThenInclude(i => i.Category)
        .AsQueryable();

        if (from.HasValue) query = query.Where(s => s.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(s => s.CreatedAt <= to.Value);
        if (type.HasValue) query = query.Where(s => s.Type == type.Value);

        var ordered = query
            .OrderByDescending(s => s.CreatedAt)
            .ThenBy(s => s.Id);

        var total = await ordered.CountAsync(ct);
        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    private IQueryable<StockMovement> InRange(DateTime? from, DateTime? to)
    {
        var query = _context.StockMovements.AsQueryable();
        if (from.HasValue) query = query.Where(s => s.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(s => s.CreatedAt <= to.Value);
        return query;
    }

    private IQueryable<StockMovement> Purchases(DateTime? from, DateTime? to)
        => InRange(from, to)
            .Where(s => s.Type == StockMovementType.AddStock && s.CostPerUnit != null);

    public async Task<decimal> GetPurchaseTotalAsync(
        DateTime? from, DateTime? to, CancellationToken ct = default)
        => await Purchases(from, to)
            .SumAsync(s => (decimal?)(s.Quantity * s.CostPerUnit), ct) ?? 0m;

    public async Task<decimal> GetLossTotalAsync(
        DateTime? from, DateTime? to, Guid? categoryId, Guid? itemId, CancellationToken ct = default)
    {
        var query = InRange(from, to)
            .Where(s =>
                (s.Type == StockMovementType.Adjustment || s.Type == StockMovementType.InventoryCount) &&
                s.Quantity < 0 &&
                (s.Reason == AdjustmentReason.Loss ||
                 s.Reason == AdjustmentReason.Damage ||
                 s.Reason == AdjustmentReason.Spoilage));

        if (itemId.HasValue) query = query.Where(s => s.ItemId == itemId.Value);
        else if (categoryId.HasValue) query = query.Where(s => s.Item.CategoryId == categoryId.Value);

        return await query.SumAsync(s => (decimal?)(-s.Quantity * s.Item.CostPrice), ct) ?? 0m;
    }

    public async Task<(IList<StockMovement> Items, int Total)> GetPurchasesPagedAsync(
        DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct = default)
    {
        var ordered = Purchases(from, to)
            .Include(s => s.Item)
            .OrderByDescending(s => s.CreatedAt)
            .ThenBy(s => s.Id);

        var total = await ordered.CountAsync(ct);
        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task AddAsync(StockMovement movement, CancellationToken ct = default) => await _context.StockMovements.AddAsync(movement, ct);
    public async Task AddRangeAsync(IList<StockMovement> movements, CancellationToken ct = default) => await _context.StockMovements.AddRangeAsync(movements, ct);
}
