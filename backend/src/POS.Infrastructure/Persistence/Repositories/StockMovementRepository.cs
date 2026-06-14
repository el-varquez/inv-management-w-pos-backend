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

    public async Task AddAsync(StockMovement movement, CancellationToken ct = default) => await _context.StockMovements.AddAsync(movement, ct);
    public async Task AddRangeAsync(IList<StockMovement> movements, CancellationToken ct = default) => await _context.StockMovements.AddRangeAsync(movements, ct);
}