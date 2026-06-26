using Microsoft.EntityFrameworkCore;
using POS.Domain.Entities;
using POS.Domain.Interfaces;

namespace POS.Infrastructure.Persistence.Repositories;

public class ItemRepository : IItemRepository
{
    private readonly AppDbContext _context;

    public ItemRepository(AppDbContext context) => _context = context;

    public async Task<Item?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Items
            .Include(i => i.Category)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task<IList<Item>> GetAllAsync(CancellationToken ct = default)
        => await _context.Items
            .Include(i => i.Category)
            .Where(i => i.IsActive)
            .OrderBy(i => i.Name)
            .ToListAsync(ct);

    public async Task<(IList<Item> Items, int Total)> GetPagedAsync(
        int page, int pageSize, bool? isComposite = null, CancellationToken ct = default)
    {
        var query = _context.Items
            .Include(i => i.Category)
            .Where(i => i.IsActive);

        if (isComposite.HasValue)
            query = query.Where(i => i.IsComposite == isComposite.Value);

        var ordered = query
            .OrderBy(i => i.Name)
            .ThenBy(i => i.Id);

        var total = await ordered.CountAsync(ct);
        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<IList<Item>> GetLowStockAsync(CancellationToken ct = default)
        => await _context.Items
            .Include(i => i.Category)
            .Where(i => i.IsActive && i.Stock <= i.LowStockThreshold)
            .ToListAsync(ct);

    public async Task AddAsync(Item item, CancellationToken ct = default)
        => await _context.Items.AddAsync(item, ct);

    public Task UpdateAsync(Item item, CancellationToken ct = default)
    {
        _context.Items.Update(item);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _context.Items.FindAsync(new object[] { id }, ct);
        if (item is not null) _context.Items.Remove(item);
    }
}