using POS.Domain.Entities;
using POS.Domain.Enums;

namespace POS.Domain.Interfaces;

public interface IStockMovementRepository
{
    Task<IList<StockMovement>> GetByItemIdAsync(Guid ItemId, CancellationToken ct = default);
    Task<IList<StockMovement>> GetAllAsync(DateTime? from, DateTime? to, StockMovementType? type, CancellationToken ct = default);
    Task AddAsync(StockMovement movement, CancellationToken ct = default);
    Task AddRangeAsync(IList<StockMovement> movements, CancellationToken ct = default);
}