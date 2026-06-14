using Microsoft.EntityFrameworkCore;
using POS.Domain.Entities;
using POS.Domain.Interfaces;

namespace POS.Infrastructure.Persistence.Repositories;

public class InventoryCountRepository : IInventoryCountRepository
{
    private readonly AppDbContext _context;

    public InventoryCountRepository(AppDbContext context) => _context = context;

    public async Task<InventoryCount?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.InventoryCounts
            .Include(ic => ic.Lines)
            .ThenInclude(l => l.Item)
            .ThenInclude(i => i.Category)
            .FirstOrDefaultAsync(ic => ic.Id == id, ct);

    public async Task<IList<InventoryCount>> GetAllAsync(CancellationToken ct = default)
        => await _context.InventoryCounts
            .Include(ic => ic.Lines)
            .OrderByDescending(ic => ic.CreatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(InventoryCount count, CancellationToken ct = default)
        => await _context.InventoryCounts.AddAsync(count, ct);

    public Task UpdateAsync(InventoryCount count, CancellationToken ct = default)
    {
        _context.InventoryCounts.Update(count);
        return Task.CompletedTask;
    }
}