using Microsoft.EntityFrameworkCore;
using POS.Domain.Entities;
using POS.Domain.Interfaces;

namespace POS.Infrastructure.Persistence.Repositories;

public class CompositeItemRepository : ICompositeItemRepository
{
    private readonly AppDbContext _context;

    public CompositeItemRepository(AppDbContext context) => _context = context;

    public async Task<IList<CompositeItem>> GetByParentIdAsync(
        Guid parentItemId, CancellationToken ct = default)
        => await _context.CompositeItems
            .Include(c => c.ComponentItem)
            .Where(c => c.ParentItemId == parentItemId)
            .ToListAsync(ct);

    public async Task AddAsync(CompositeItem compositeItem, CancellationToken ct = default)
        => await _context.CompositeItems.AddAsync(compositeItem, ct);

    public async Task DeleteByParentIdAsync(Guid parentItemId, CancellationToken ct = default)
    {
        var components = await _context.CompositeItems
            .Where(c => c.ParentItemId == parentItemId)
            .ToListAsync(ct);
        _context.CompositeItems.RemoveRange(components);
    }
}