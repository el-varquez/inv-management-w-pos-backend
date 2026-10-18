using POS.Domain.Entities;
using POS.Domain.Enums;

namespace POS.Domain.Interfaces;

public interface IStockMovementRepository
{
    Task<IList<StockMovement>> GetByItemIdAsync(Guid ItemId, CancellationToken ct = default);
    Task<IList<StockMovement>> GetAllAsync(DateTime? from, DateTime? to, StockMovementType? type, CancellationToken ct = default);
    Task<(IList<StockMovement> Items, int Total)> GetPagedAsync(DateTime? from, DateTime? to, StockMovementType? type, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Sum of Quantity × CostPerUnit over AddStock rows that carry a cost.</summary>
    Task<decimal> GetPurchaseTotalAsync(DateTime? from, DateTime? to, CancellationToken ct = default);

    /// <summary>
    /// Sum of −Quantity × Item.CostPrice over negative Adjustment / InventoryCount rows whose
    /// reason is Loss, Damage or Spoilage, optionally narrowed to one item or one category.
    /// </summary>
    Task<decimal> GetLossTotalAsync(DateTime? from, DateTime? to, Guid? categoryId, Guid? itemId, CancellationToken ct = default);

    /// <summary>AddStock rows with a cost, newest first, with Item loaded.</summary>
    Task<(IList<StockMovement> Items, int Total)> GetPurchasesPagedAsync(DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct = default);

    Task AddAsync(StockMovement movement, CancellationToken ct = default);
    Task AddRangeAsync(IList<StockMovement> movements, CancellationToken ct = default);
}
