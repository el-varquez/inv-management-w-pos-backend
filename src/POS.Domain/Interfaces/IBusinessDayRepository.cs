using POS.Domain.Entities;

namespace POS.Domain.Interfaces;

public interface IBusinessDayRepository
{
    Task<BusinessDay?> GetOpenAsync(CancellationToken ct = default);
    Task<BusinessDay?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<int> GetLastNumberAsync(CancellationToken ct = default);
    Task<(IList<BusinessDay> Items, int Total)> GetPagedAsync(
        int page, int pageSize, CancellationToken ct = default);
    Task<IList<Shift>> GetShiftsAsync(Guid dayId, CancellationToken ct = default);
    Task AddAsync(BusinessDay day, CancellationToken ct = default);
    Task UpdateAsync(BusinessDay day, CancellationToken ct = default);
}
