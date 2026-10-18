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

    private IQueryable<Sale> InRange(DateTime? from, DateTime? to)
    {
        var query = _context.Sales.AsQueryable();
        if (from.HasValue) query = query.Where(t => t.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(t => t.CreatedAt <= to.Value);
        return query;
    }

    public async Task<SalesTotals> GetTotalsAsync(
        DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var query = InRange(from, to);

        var gross = await query.SumAsync(t => t.RefundedFromId == null ? (decimal?)t.Subtotal : 0m, ct) ?? 0m;
        var discounts = await query.SumAsync(t => t.RefundedFromId == null ? (decimal?)t.DiscountAmount : 0m, ct) ?? 0m;
        var paidTotal = await query.SumAsync(t => t.RefundedFromId == null ? (decimal?)t.Total : 0m, ct) ?? 0m;
        var refundTotal = await query.SumAsync(t => t.RefundedFromId != null ? (decimal?)t.Total : 0m, ct) ?? 0m;
        var count = await query.CountAsync(t => t.RefundedFromId == null, ct);

        var refunds = Math.Abs(refundTotal);
        return new SalesTotals(gross, discounts, refunds, paidTotal - refunds, count);
    }

    public async Task<(IList<DailySalesRow> Rows, int Total)> GetDailyRowsPagedAsync(
        DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct = default)
    {
        var grouped = InRange(from, to)
            .GroupBy(t => t.CreatedAt.Date)
            .Select(g => new
            {
                Date = g.Key,
                Gross = g.Sum(t => t.RefundedFromId == null ? t.Subtotal : 0m),
                Discounts = g.Sum(t => t.RefundedFromId == null ? t.DiscountAmount : 0m),
                PaidTotal = g.Sum(t => t.RefundedFromId == null ? t.Total : 0m),
                RefundTotal = g.Sum(t => t.RefundedFromId != null ? t.Total : 0m),
                Count = g.Sum(t => t.RefundedFromId == null ? 1 : 0)
            })
            .OrderBy(r => r.Date);

        var total = await grouped.CountAsync(ct);
        var rows = await grouped
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var result = rows
            .Select(r =>
            {
                var refunds = Math.Abs(r.RefundTotal);
                return new DailySalesRow(r.Date, r.Gross, r.Discounts, refunds, r.PaidTotal - refunds, r.Count);
            })
            .ToList();

        return (result, total);
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
