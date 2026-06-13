using POS.Domain.Entities;

namespace POS.Domain.Interfaces;

public interface IItemRepository
{
    Task<Item?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IList<Item>> GetAllAsync(CancellationToken ct = default);
    Task<IList<Item>> GetLowStockAsync(CancellationToken ct = default);
    Task AddAsync(Item item, CancellationToken ct = default);
    Task UpdateAsync(Item item, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}