using Microsoft.EntityFrameworkCore;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Domain.Interfaces;

namespace POS.Infrastructure.Persistence.Repositories;

public class BusinessDayRepository : IBusinessDayRepository
{
    private readonly AppDbContext _ctx;
    public BusinessDayRepository(AppDbContext ctx) => _ctx = ctx;

    public Task<BusinessDay?> GetOpenAsync(CancellationToken ct = default)
        => _ctx.BusinessDays.SingleOrDefaultAsync(d => d.Status == DayStatus.Open, ct);

    public Task<BusinessDay?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _ctx.BusinessDays.SingleOrDefaultAsync(d => d.Id == id, ct);

    public async Task<int> GetLastNumberAsync(CancellationToken ct = default)
        => await _ctx.BusinessDays.AnyAsync(ct)
            ? await _ctx.BusinessDays.MaxAsync(d => d.Number, ct)
            : 0;

    public async Task<(IList<BusinessDay> Items, int Total)> GetPagedAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var ordered = _ctx.BusinessDays
            .Include(d => d.Shifts)
            .OrderByDescending(d => d.Number);
        var total = await ordered.CountAsync(ct);
        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return (items, total);
    }

    public async Task<IList<Shift>> GetShiftsAsync(Guid dayId, CancellationToken ct = default)
        => await _ctx.Shifts
            .Where(s => s.BusinessDayId == dayId)
            .OrderBy(s => s.Number)
            .ToListAsync(ct);

    public async Task AddAsync(BusinessDay day, CancellationToken ct = default)
        => await _ctx.BusinessDays.AddAsync(day, ct);

    public Task UpdateAsync(BusinessDay day, CancellationToken ct = default)
    {
        _ctx.BusinessDays.Update(day);
        return Task.CompletedTask;
    }
}
