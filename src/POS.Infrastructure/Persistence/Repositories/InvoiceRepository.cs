using Microsoft.EntityFrameworkCore;
using POS.Domain.Entities;
using POS.Domain.Interfaces;

namespace POS.Infrastructure.Persistence.Repositories;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly AppDbContext _context;
    public InvoiceRepository(AppDbContext context) => _context = context;

    public async Task<Invoice?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Invoices
            .Include(i => i.Items)
            .Include(i => i.Suki)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task<IList<Invoice>> GetAllAsync(
        DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var query = _context.Invoices
            .Include(i => i.Items)
            .Include(i => i.Suki)
            .AsQueryable();

        if (from.HasValue) query = query.Where(i => i.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(i => i.CreatedAt <= to.Value);

        return await query.OrderByDescending(i => i.CreatedAt).ToListAsync(ct);
    }

    public async Task<(IList<Invoice> Items, int Total)> GetPagedAsync(
        DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Invoices.Include(i => i.Suki).AsQueryable();

        if (from.HasValue) query = query.Where(i => i.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(i => i.CreatedAt <= to.Value);

        var ordered = query
            .OrderByDescending(i => i.CreatedAt)
            .ThenBy(i => i.Id);

        var total = await ordered.CountAsync(ct);
        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<IList<Invoice>> GetBySukiAsync(Guid sukiId, CancellationToken ct = default)
        => await _context.Invoices
            .Where(i => i.SukiId == sukiId)
            .OrderBy(i => i.CreatedAt)
            .ToListAsync(ct);

    public async Task<int> GetMaxInvoiceSequenceAsync(string prefix, CancellationToken ct = default)
    {
        var numbers = await _context.Invoices
            .Where(i => i.InvoiceNumber.StartsWith(prefix))
            .Select(i => i.InvoiceNumber)
            .ToListAsync(ct);
        return numbers.Count == 0
            ? 0
            : numbers.Max(n => int.TryParse(n[prefix.Length..], out var seq) ? seq : 0);
    }

    public async Task AddAsync(Invoice invoice, CancellationToken ct = default)
        => await _context.Invoices.AddAsync(invoice, ct);
}
