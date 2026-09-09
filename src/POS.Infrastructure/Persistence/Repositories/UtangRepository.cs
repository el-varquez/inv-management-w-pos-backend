using Microsoft.EntityFrameworkCore;
using POS.Domain.Entities;
using POS.Domain.Interfaces;

namespace POS.Infrastructure.Persistence.Repositories;

public class UtangRepository : IUtangRepository
{
    private readonly AppDbContext _context;
    public UtangRepository(AppDbContext context) => _context = context;

    public async Task<Suki?> GetSukiByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Sukis.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<(IList<SukiWithBalance> Items, int Total)> GetSukisPagedAsync(
        string? term, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Sukis.AsQueryable();
        if (!string.IsNullOrWhiteSpace(term))
        {
            var lowered = term.Trim().ToLower();
            query = query.Where(s =>
                s.Name.ToLower().Contains(lowered) ||
                (s.Phone != null && s.Phone.Contains(lowered)));
        }

        var ordered = query.OrderBy(s => s.Name).ThenBy(s => s.Id);
        var total = await ordered.CountAsync(ct);
        var sukis = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (await AttachLedgersAsync(sukis, ct), total);
    }

    public async Task<IList<SukiWithBalance>> GetAllSukiBalancesAsync(
        CancellationToken ct = default)
        => await AttachLedgersAsync(
            await _context.Sukis.OrderBy(s => s.Name).ToListAsync(ct), ct);

    private async Task<IList<SukiWithBalance>> AttachLedgersAsync(
        IList<Suki> sukis, CancellationToken ct)
    {
        var ids = sukis.Select(s => s.Id).ToList();
        var invoices = await _context.Invoices
            .Where(i => ids.Contains(i.SukiId))
            .OrderBy(i => i.CreatedAt)
            .ToListAsync(ct);
        var payments = await _context.Payments
            .Where(p => ids.Contains(p.SukiId))
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(ct);
        var invoicesBySuki = invoices.ToLookup(i => i.SukiId);
        var paymentsBySuki = payments.ToLookup(p => p.SukiId);

        return sukis
            .Select(s =>
            {
                var live = invoicesBySuki[s.Id].Where(i => !i.IsVoided).ToList();
                var paid = paymentsBySuki[s.Id].Where(p => !p.IsVoided).Sum(p => p.Amount);
                return new SukiWithBalance(
                    s,
                    live.Sum(i => i.Total) - paid,
                    live.Count,
                    live.Count == 0 ? null : live.Min(i => i.CreatedAt),
                    invoicesBySuki[s.Id].ToList(),
                    paymentsBySuki[s.Id].ToList());
            })
            .ToList();
    }

    public async Task AddSukiAsync(Suki suki, CancellationToken ct = default)
        => await _context.Sukis.AddAsync(suki, ct);

    public async Task<bool> HasLedgerHistoryAsync(Guid sukiId, CancellationToken ct = default)
        => await _context.Invoices.AnyAsync(i => i.SukiId == sukiId, ct)
            || await _context.Payments.AnyAsync(p => p.SukiId == sukiId, ct);

    public async Task DeleteSukiAsync(Guid id, CancellationToken ct = default)
    {
        var suki = await _context.Sukis.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (suki is not null) _context.Sukis.Remove(suki);
    }

    public async Task<Payment?> GetPaymentByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Payments.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IList<Payment>> GetPaymentsBySukiAsync(
        Guid sukiId, CancellationToken ct = default)
        => await _context.Payments
            .Where(p => p.SukiId == sukiId)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(ct);

    public async Task<IList<Payment>> GetPaymentsSinceAsync(
        DateTime fromUtc, CancellationToken ct = default)
        => await _context.Payments
            .Where(p => !p.IsVoided && p.CreatedAt >= fromUtc)
            .ToListAsync(ct);

    public async Task<IList<Payment>> GetPaymentsInRangeAsync(
        DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default)
    {
        var query = _context.Payments.AsQueryable();

        if (fromUtc.HasValue) query = query.Where(p => p.CreatedAt >= fromUtc.Value);
        if (toUtc.HasValue) query = query.Where(p => p.CreatedAt <= toUtc.Value);

        return await query.ToListAsync(ct);
    }

    public async Task AddPaymentAsync(Payment payment, CancellationToken ct = default)
        => await _context.Payments.AddAsync(payment, ct);

    public async Task<decimal> GetBalanceAsync(Guid sukiId, CancellationToken ct = default)
    {
        var invoiced = await _context.Invoices
            .Where(i => i.SukiId == sukiId && !i.IsVoided)
            .SumAsync(i => (decimal?)i.Total, ct) ?? 0m;
        var paid = await _context.Payments
            .Where(p => p.SukiId == sukiId && !p.IsVoided)
            .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;
        return invoiced - paid;
    }
}
