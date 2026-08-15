using POS.Domain.Entities;

namespace POS.Domain.Interfaces;

public interface IShiftRepository
{
    Task<Shift?> GetOpenAsync(CancellationToken ct = default);
    Task<Shift?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<int> GetLastNumberAsync(CancellationToken ct = default);
    Task<(IList<Shift> Items, int Total)> GetPagedAsync(
        int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(Shift shift, CancellationToken ct = default);
    Task UpdateAsync(Shift shift, CancellationToken ct = default);

    Task<IList<CashDrawerMovement>> GetMovementsAsync(
        Guid shiftId, CancellationToken ct = default);
    Task<CashDrawerMovement?> GetMovementByIdAsync(
        Guid id, CancellationToken ct = default);
    Task AddMovementAsync(
        CashDrawerMovement movement, CancellationToken ct = default);
}
