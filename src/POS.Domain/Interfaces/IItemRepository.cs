using POS.Domain.Entities;

namespace POS.Domain.Interfaces;

public interface IItemRepository
{
    Task<Item?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Item?> GetByBarcodeAsync(string barcode, CancellationToken ct = default);
    Task<Item?> GetByItemCodeAsync(string itemCode, CancellationToken ct = default);
    Task<Item?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<IList<string>> GetItemCodesAsync(CancellationToken ct = default);
    Task<IList<Item>> SearchAsync(string term, int limit, Guid? categoryId = null, CancellationToken ct = default);
    Task<IList<Item>> SearchActiveAsync(string term, int limit, CancellationToken ct = default);
    Task<IList<Item>> GetAllAsync(CancellationToken ct = default);
    Task<(IList<Item> Items, int Total)> GetPagedAsync(int page, int pageSize, bool? isComposite = null, bool? tracksStock = null, string? search = null, CancellationToken ct = default);
    Task<(IList<Item> Items, int Total)> GetLowStockPagedAsync(int page, int pageSize, string? search = null, CancellationToken ct = default);
    Task<(IList<Item> Items, int Total, decimal TotalValue)> GetValuationPagedAsync(int page, int pageSize, string? search = null, CancellationToken ct = default);
    Task AddAsync(Item item, CancellationToken ct = default);
    Task UpdateAsync(Item item, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<(bool HasSales, bool HasCountLines, bool IsComponent)> GetDeleteBlockersAsync(Guid id, CancellationToken ct = default);
}
