using POS.Domain.Entities;

namespace POS.Domain.Interfaces;

public interface ICompositeItemRepository
{
    Task<IList<CompositeItem>> GetByParentIdAsync(Guid parentItemId, CancellationToken ct = default);
    Task AddAsync(CompositeItem compositeItem, CancellationToken ct = default);
    Task DeleteByParentIdAsync(Guid parentItemId, CancellationToken ct = default);
}