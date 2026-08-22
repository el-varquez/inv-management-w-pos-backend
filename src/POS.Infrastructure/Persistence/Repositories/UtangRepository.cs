using Microsoft.EntityFrameworkCore;
using POS.Domain.Entities;
using POS.Domain.Enums;
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

        return (await AttachBalancesAsync(sukis, ct), total);
    }

    public async Task<IList<SukiWithBalance>> GetAllSukiBalancesAsync(
        CancellationToken ct = default)
        => await AttachBalancesAsync(
            await _context.Sukis.OrderBy(s => s.Name).ToListAsync(ct), ct);

    private async Task<IList<SukiWithBalance>> AttachBalancesAsync(
        IList<Suki> sukis, CancellationToken ct)
    {
        var ids = sukis.Select(s => s.Id).ToList();
        var totals = await _context.UtangEntries
            .Where(e => !e.IsVoided && ids.Contains(e.SukiId))
            .GroupBy(e => e.SukiId)
            .Select(g => new
            {
                SukiId = g.Key,
                Balance = g.Sum(e =>
                    e.Type == UtangEntryType.Charge ? e.Amount : -e.Amount),
                ChargeCount = g.Count(e => e.Type == UtangEntryType.Charge),
                OldestChargeAt = g
                    .Where(e => e.Type == UtangEntryType.Charge)
                    .Min(e => (DateTime?)e.CreatedAt)
            })
            .ToListAsync(ct);

        var byId = totals.ToDictionary(t => t.SukiId);
        return sukis
            .Select(s => byId.TryGetValue(s.Id, out var t)
                ? new SukiWithBalance(s, t.Balance, t.ChargeCount, t.OldestChargeAt)
                : new SukiWithBalance(s, 0m, 0, null))
            .ToList();
    }

    public async Task AddSukiAsync(Suki suki, CancellationToken ct = default)
        => await _context.Sukis.AddAsync(suki, ct);

    public async Task<UtangEntry?> GetEntryByIdAsync(
        Guid id, CancellationToken ct = default)
        => await _context.UtangEntries.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IList<UtangEntry>> GetEntriesBySukiAsync(
        Guid sukiId, CancellationToken ct = default)
        => await _context.UtangEntries
            .Include(e => e.Transaction)
            .Where(e => e.SukiId == sukiId)
            .OrderBy(e => e.CreatedAt)
            .ThenBy(e => e.Id)
            .ToListAsync(ct);

    public async Task<IList<UtangEntry>> GetEntriesByShiftAsync(
        Guid shiftId, CancellationToken ct = default)
        => await _context.UtangEntries
            .Where(e => e.ShiftId == shiftId)
            .ToListAsync(ct);

    public async Task<IList<UtangEntry>> GetEntriesByTransactionAsync(
        Guid transactionId, CancellationToken ct = default)
        => await _context.UtangEntries
            .Where(e => e.TransactionId == transactionId)
            .ToListAsync(ct);

    public async Task<decimal> GetBalanceAsync(
        Guid sukiId, CancellationToken ct = default)
        => await _context.UtangEntries
            .Where(e => e.SukiId == sukiId && !e.IsVoided)
            .Select(e => e.Type == UtangEntryType.Charge ? e.Amount : -e.Amount)
            .SumAsync(ct);

    public async Task<IList<UtangEntry>> GetPaymentsSinceAsync(
        DateTime fromUtc, CancellationToken ct = default)
        => await _context.UtangEntries
            .Where(e => e.Type == UtangEntryType.Payment
                && !e.IsVoided
                && e.CreatedAt >= fromUtc)
            .ToListAsync(ct);

    public async Task AddEntryAsync(UtangEntry entry, CancellationToken ct = default)
        => await _context.UtangEntries.AddAsync(entry, ct);
}
