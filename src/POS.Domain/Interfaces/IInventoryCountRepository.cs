using POS.Domain.Entities;
using POS.Domain.Enums;

namespace POS.Domain.Interfaces;

public interface IInventoryCountRepository
{
    Task<InventoryCount?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IList<InventoryCount>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(InventoryCount count, CancellationToken ct = default);
    Task UpdateAsync(InventoryCount count, CancellationToken ct = default);
    Task<(IList<InventoryCount> Items, int Total)> GetPagedAsync(
        InventoryCountStatus? status, int page, int pageSize, CancellationToken ct = default);
}