using Microsoft.EntityFrameworkCore;
using POS.Domain.Entities;
using POS.Domain.Interfaces;

namespace POS.Infrastructure.Persistence.Repositories;

public class SaleRepository : ISaleRepository
{
    private readonly AppDbContext _context;

    public SaleRepository(AppDbContext context) => _context = context;

    public async Task<Sale?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Sales
            .Include(t => t.Items)
            .Include(t => t.PaymentMethod)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IList<Sale>> GetAllAsync(
        DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var query = _context.Sales
            .Include(t => t.Items)
            .Include(t => t.PaymentMethod)
            .AsQueryable();

        if (from.HasValue) query = query.Where(t => t.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(t => t.CreatedAt <= to.Value);

        return await query.OrderByDescending(t => t.CreatedAt).ToListAsync(ct);
    }

    public async Task<IList<Sale>> GetAllWithItemCategoriesAsync(
        DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var query = _context.Sales
            .Include(t => t.Items)
                .ThenInclude(i => i.Item)
                    .ThenInclude(it => it.Category)
            .Include(t => t.PaymentMethod)
            .AsQueryable();

        if (from.HasValue) query = query.Where(t => t.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(t => t.CreatedAt <= to.Value);

        return await query.OrderByDescending(t => t.CreatedAt).ToListAsync(ct);
    }

    public async Task<(IList<Sale> Items, int Total)> GetPagedAsync(
        DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Sales
            .Include(t => t.Items)
            .Include(t => t.PaymentMethod)
            .AsQueryable();

        if (from.HasValue) query = query.Where(t => t.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(t => t.CreatedAt <= to.Value);

        var ordered = query
            .OrderByDescending(t => t.CreatedAt)
            .ThenBy(t => t.Id);

        var total = await ordered.CountAsync(ct);
        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<IList<Sale>> GetByShiftAsync(
        Guid shiftId, CancellationToken ct = default)
        => await _context.Sales
            .Include(t => t.PaymentMethod)
            .Where(t => t.ShiftId == shiftId)
            .ToListAsync(ct);

    public async Task<int> GetMaxReceiptSequenceAsync(string prefix, CancellationToken ct = default)
    {
        var numbers = await _context.Sales
            .Where(t => t.ReceiptNumber.StartsWith(prefix))
            .Select(t => t.ReceiptNumber)
            .ToListAsync(ct);
        return numbers.Count == 0
            ? 0
            : numbers.Max(n => int.TryParse(n[prefix.Length..], out var seq) ? seq : 0);
    }

    public async Task AddAsync(Sale sale, CancellationToken ct = default)
        => await _context.Sales.AddAsync(sale, ct);

    public Task UpdateAsync(Sale sale, CancellationToken ct = default)
    {
        _context.Sales.Update(sale);
        return Task.CompletedTask;
    }
}