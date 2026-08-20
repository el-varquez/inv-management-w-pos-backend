using Microsoft.EntityFrameworkCore;
using POS.Domain.Entities;
using POS.Domain.Interfaces;

namespace POS.Infrastructure.Persistence.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly AppDbContext _context;

    public TransactionRepository(AppDbContext context) => _context = context;

    public async Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Transactions
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IList<Transaction>> GetAllAsync(
        DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var query = _context.Transactions
            .Include(t => t.Items)
            .AsQueryable();

        if (from.HasValue) query = query.Where(t => t.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(t => t.CreatedAt <= to.Value);

        return await query.OrderByDescending(t => t.CreatedAt).ToListAsync(ct);
    }

    public async Task<IList<Transaction>> GetAllWithItemCategoriesAsync(
        DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var query = _context.Transactions
            .Include(t => t.Items)
                .ThenInclude(i => i.Item)
                    .ThenInclude(it => it.Category)
            .AsQueryable();

        if (from.HasValue) query = query.Where(t => t.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(t => t.CreatedAt <= to.Value);

        return await query.OrderByDescending(t => t.CreatedAt).ToListAsync(ct);
    }

    public async Task<(IList<Transaction> Items, int Total)> GetPagedAsync(
        DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Transactions
            .Include(t => t.Items)
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

    public async Task<IList<Transaction>> GetByShiftAsync(
        Guid shiftId, CancellationToken ct = default)
        => await _context.Transactions
            .Where(t => t.ShiftId == shiftId)
            .ToListAsync(ct);

    public async Task<int> GetMaxReceiptSequenceAsync(string prefix, CancellationToken ct = default)
    {
        var numbers = await _context.Transactions
            .Where(t => t.ReceiptNumber.StartsWith(prefix))
            .Select(t => t.ReceiptNumber)
            .ToListAsync(ct);
        return numbers.Count == 0
            ? 0
            : numbers.Max(n => int.TryParse(n[prefix.Length..], out var seq) ? seq : 0);
    }

    public async Task AddAsync(Transaction transaction, CancellationToken ct = default)
        => await _context.Transactions.AddAsync(transaction, ct);

    public Task UpdateAsync(Transaction transaction, CancellationToken ct = default)
    {
        _context.Transactions.Update(transaction);
        return Task.CompletedTask;
    }
}