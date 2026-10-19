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

    private IQueryable<SaleItem> LinesInRange(DateTime? from, DateTime? to, Guid? categoryId, Guid? itemId)
    {
        var query = _context.SaleItems.AsQueryable();
        if (from.HasValue) query = query.Where(l => l.Sale.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(l => l.Sale.CreatedAt <= to.Value);
        if (itemId.HasValue) query = query.Where(l => l.ItemId == itemId.Value);
        else if (categoryId.HasValue) query = query.Where(l => l.Item.CategoryId == categoryId.Value);
        return query;
    }

    public async Task<SaleLineTotals> GetLineTotalsAsync(
        DateTime? from, DateTime? to, Guid? categoryId, Guid? itemId, CancellationToken ct = default)
    {
        var lines = LinesInRange(from, to, categoryId, itemId);
        var revenue = await lines.SumAsync(l => (decimal?)l.Total, ct) ?? 0m;
        var cost = await lines.SumAsync(l => (decimal?)(l.CostPrice * l.Quantity), ct) ?? 0m;
        return new SaleLineTotals(revenue, cost);
    }

    public async Task<(IList<ItemProfitRow> Rows, int Total)> GetItemProfitRowsPagedAsync(
        DateTime? from, DateTime? to, Guid? categoryId, Guid? itemId, int page, int pageSize, CancellationToken ct = default)
    {
        var grouped = LinesInRange(from, to, categoryId, itemId)
            .GroupBy(l => new { l.ItemId, l.Item.Name, CategoryName = l.Item.Category.Name })
            .Select(g => new
            {
                g.Key.ItemId,
                g.Key.Name,
                g.Key.CategoryName,
                Quantity = g.Sum(l => l.Quantity),
                Revenue = g.Sum(l => l.Total),
                Cost = g.Sum(l => l.CostPrice * l.Quantity)
            })
            .OrderByDescending(r => (double)(r.Revenue - r.Cost)) // SQLite cannot ORDER BY a decimal expression; the sort key alone is cast
            .ThenBy(r => r.ItemId);

        var total = await grouped.CountAsync(ct);
        var rows = await grouped
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (rows
            .Select(r => new ItemProfitRow(r.ItemId, r.Name, r.CategoryName, r.Quantity, r.Revenue, r.Cost))
            .ToList(), total);
    }

    public async Task<IList<ItemSalesRow>> GetItemSalesRowsAsync(
        DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var rows = await LinesInRange(from, to, null, null)
            .GroupBy(l => new { l.ItemId, l.Item.Name })
            .Select(g => new
            {
                g.Key.ItemId,
                g.Key.Name,
                Quantity = g.Sum(l => l.Quantity),
                Revenue = g.Sum(l => l.Total),
                Profit = g.Sum(l => l.Total - l.CostPrice * l.Quantity)
            })
            .ToListAsync(ct);

        return rows
            .Select(r => new ItemSalesRow(r.ItemId, r.Name, r.Quantity, r.Revenue, r.Profit))
            .ToList();
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
