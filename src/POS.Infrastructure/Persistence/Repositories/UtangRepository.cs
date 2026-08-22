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
        var chargeTotals = await _context.UtangCharges
            .Where(c => !c.IsVoided && ids.Contains(c.SukiId))
            .GroupBy(c => c.SukiId)
            .Select(g => new
            {
                SukiId = g.Key,
                Charged = g.Sum(c => c.Amount),
                Count = g.Count(),
                Oldest = g.Min(c => (DateTime?)c.CreatedAt)
            })
            .ToListAsync(ct);
        var paymentTotals = await _context.UtangPayments
            .Where(p => !p.IsVoided && ids.Contains(p.SukiId))
            .GroupBy(p => p.SukiId)
            .Select(g => new { SukiId = g.Key, Paid = g.Sum(p => p.Amount) })
            .ToListAsync(ct);

        var charges = chargeTotals.ToDictionary(t => t.SukiId);
        var payments = paymentTotals.ToDictionary(t => t.SukiId);
        return sukis
            .Select(s =>
            {
                var c = charges.GetValueOrDefault(s.Id);
                var paid = payments.GetValueOrDefault(s.Id)?.Paid ?? 0m;
                return new SukiWithBalance(
                    s, (c?.Charged ?? 0m) - paid, c?.Count ?? 0, c?.Oldest);
            })
            .ToList();
    }

    public async Task AddSukiAsync(Suki suki, CancellationToken ct = default)
        => await _context.Sukis.AddAsync(suki, ct);

    public async Task<IList<UtangCharge>> GetChargesBySukiAsync(
        Guid sukiId, CancellationToken ct = default)
        => await _context.UtangCharges
            .Include(c => c.Transaction)
            .Where(c => c.SukiId == sukiId)
            .ToListAsync(ct);

    public async Task<IList<UtangCharge>> GetChargesByShiftAsync(
        Guid shiftId, CancellationToken ct = default)
        => await _context.UtangCharges
            .Where(c => c.ShiftId == shiftId)
            .ToListAsync(ct);

    public async Task<IList<UtangCharge>> GetChargesByTransactionAsync(
        Guid transactionId, CancellationToken ct = default)
        => await _context.UtangCharges
            .Where(c => c.TransactionId == transactionId)
            .ToListAsync(ct);

    public async Task<IList<UtangCharge>> GetChargesInRangeAsync(
        DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default)
    {
        var query = _context.UtangCharges
            .Include(c => c.Suki)
            .AsQueryable();

        if (fromUtc.HasValue) query = query.Where(c => c.CreatedAt >= fromUtc.Value);
        if (toUtc.HasValue) query = query.Where(c => c.CreatedAt <= toUtc.Value);

        return await query.ToListAsync(ct);
    }

    public async Task AddChargeAsync(UtangCharge charge, CancellationToken ct = default)
        => await _context.UtangCharges.AddAsync(charge, ct);

    public async Task<UtangPayment?> GetPaymentByIdAsync(
        Guid id, CancellationToken ct = default)
        => await _context.UtangPayments.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IList<UtangPayment>> GetPaymentsBySukiAsync(
        Guid sukiId, CancellationToken ct = default)
        => await _context.UtangPayments
            .Include(p => p.Transaction)
            .Where(p => p.SukiId == sukiId)
            .ToListAsync(ct);

    public async Task<IList<UtangPayment>> GetPaymentsByShiftAsync(
        Guid shiftId, CancellationToken ct = default)
        => await _context.UtangPayments
            .Where(p => p.ShiftId == shiftId)
            .ToListAsync(ct);

    public async Task<IList<UtangPayment>> GetPaymentsByTransactionAsync(
        Guid transactionId, CancellationToken ct = default)
        => await _context.UtangPayments
            .Where(p => p.TransactionId == transactionId)
            .ToListAsync(ct);

    public async Task<IList<UtangPayment>> GetPaymentsSinceAsync(
        DateTime fromUtc, CancellationToken ct = default)
        => await _context.UtangPayments
            .Where(p => !p.IsVoided && p.CreatedAt >= fromUtc)
            .ToListAsync(ct);

    public async Task<IList<UtangPayment>> GetPaymentsInRangeAsync(
        DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default)
    {
        var query = _context.UtangPayments.AsQueryable();

        if (fromUtc.HasValue) query = query.Where(p => p.CreatedAt >= fromUtc.Value);
        if (toUtc.HasValue) query = query.Where(p => p.CreatedAt <= toUtc.Value);

        return await query.ToListAsync(ct);
    }

    public async Task AddPaymentAsync(UtangPayment payment, CancellationToken ct = default)
        => await _context.UtangPayments.AddAsync(payment, ct);

    public async Task<decimal> GetBalanceAsync(
        Guid sukiId, CancellationToken ct = default)
    {
        var charged = await _context.UtangCharges
            .Where(c => c.SukiId == sukiId && !c.IsVoided)
            .SumAsync(c => (decimal?)c.Amount, ct) ?? 0m;
        var paid = await _context.UtangPayments
            .Where(p => p.SukiId == sukiId && !p.IsVoided)
            .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;
        return charged - paid;
    }
}
