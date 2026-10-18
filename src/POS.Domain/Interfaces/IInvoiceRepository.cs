using POS.Domain.Entities;

namespace POS.Domain.Interfaces;

/// <summary>Quantity charged per item on live (non-voided) invoices, aggregated in SQL.</summary>
public record ItemQuantityRow(Guid ItemId, string ItemName, int Quantity);

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IList<Invoice>> GetAllAsync(
        DateTime? from, DateTime? to, CancellationToken ct = default);
    Task<(IList<Invoice> Items, int Total)> GetPagedAsync(
        DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct = default);
    Task<IList<Invoice>> GetBySukiAsync(Guid sukiId, CancellationToken ct = default);
    Task<IList<ItemQuantityRow>> GetItemQuantitiesAsync(
        DateTime? from, DateTime? to, CancellationToken ct = default);
    Task<int> GetMaxInvoiceSequenceAsync(string prefix, CancellationToken ct = default);
    Task AddAsync(Invoice invoice, CancellationToken ct = default);
}
