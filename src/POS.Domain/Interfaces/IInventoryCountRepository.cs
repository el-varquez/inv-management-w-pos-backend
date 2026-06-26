using POS.Domain.Entities;

namespace POS.Domain.Interfaces;

public interface IInventoryCountRepository
{
    Task<InventoryCount?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IList<InventoryCount>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(InventoryCount count, CancellationToken ct = default);
    Task UpdateAsync(InventoryCount count, CancellationToken ct = default);
}