using Microsoft.EntityFrameworkCore;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Domain.Interfaces;

namespace POS.Infrastructure.Persistence.Repositories;

public class ShiftRepository : IShiftRepository
{
    private readonly AppDbContext _ctx;
    public ShiftRepository(AppDbContext ctx) => _ctx = ctx;

    public Task<Shift?> GetOpenAsync(CancellationToken ct = default)
        => _ctx.Shifts.SingleOrDefaultAsync(s => s.Status == ShiftStatus.Open, ct);

    public Task<Shift?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _ctx.Shifts.SingleOrDefaultAsync(s => s.Id == id, ct);

    public async Task<int> GetLastNumberAsync(CancellationToken ct = default)
        => await _ctx.Shifts.AnyAsync(ct)
            ? await _ctx.Shifts.MaxAsync(s => s.Number, ct)
            : 0;

    public async Task<(IList<Shift> Items, int Total)> GetPagedAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var ordered = _ctx.Shifts.OrderByDescending(s => s.Number);
        var total = await ordered.CountAsync(ct);
        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return (items, total);
    }

    public async Task AddAsync(Shift shift, CancellationToken ct = default)
        => await _ctx.Shifts.AddAsync(shift, ct);

    public Task UpdateAsync(Shift shift, CancellationToken ct = default)
    {
        _ctx.Shifts.Update(shift);
        return Task.CompletedTask;
    }

    public async Task<IList<CashDrawerMovement>> GetMovementsAsync(
        Guid shiftId, CancellationToken ct = default)
        => await _ctx.CashDrawerMovements
            .Where(m => m.ShiftId == shiftId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);

    public Task<CashDrawerMovement?> GetMovementByIdAsync(
        Guid id, CancellationToken ct = default)
        => _ctx.CashDrawerMovements.SingleOrDefaultAsync(m => m.Id == id, ct);

    public async Task AddMovementAsync(
        CashDrawerMovement movement, CancellationToken ct = default)
        => await _ctx.CashDrawerMovements.AddAsync(movement, ct);

    public async Task<IList<EWalletTransaction>> GetEWalletTransactionsAsync(
        Guid shiftId, CancellationToken ct = default)
        => await _ctx.EWalletTransactions
            .Where(t => t.ShiftId == shiftId)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(ct);

    public Task<EWalletTransaction?> GetEWalletTransactionByIdAsync(
        Guid id, CancellationToken ct = default)
        => _ctx.EWalletTransactions.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<EWalletTransaction?> GetEWalletTransactionByFeeAsync(
        Guid feeTransactionId, CancellationToken ct = default)
        => _ctx.EWalletTransactions
            .FirstOrDefaultAsync(t => t.FeeTransactionId == feeTransactionId, ct);

    public async Task AddEWalletTransactionAsync(
        EWalletTransaction transaction, CancellationToken ct = default)
        => await _ctx.EWalletTransactions.AddAsync(transaction, ct);
}
