using POS.Domain.Entities;

namespace POS.Domain.Interfaces;

/// <summary>Range totals over the sales table. Paid sales have no RefundedFromId; refunds are the rows that do.</summary>
public record SalesTotals(decimal Gross, decimal Discounts, decimal Refunds, decimal Net, int Count);

/// <summary>One calendar day of the sales report, aggregated in SQL.</summary>
public record DailySalesRow(DateTime Date, decimal Gross, decimal Discounts, decimal Refunds, decimal Net, int Count);

public interface ISaleRepository
{
    Task<Sale?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IList<Sale>> GetAllAsync(
        DateTime? from,
        DateTime? to,
        CancellationToken ct = default);
    Task<IList<Sale>> GetAllWithItemCategoriesAsync(
        DateTime? from,
        DateTime? to,
        CancellationToken ct = default);
    Task<(IList<Sale> Items, int Total)> GetPagedAsync(
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken ct = default);
    Task<SalesTotals> GetTotalsAsync(DateTime? from, DateTime? to, CancellationToken ct = default);
    Task<(IList<DailySalesRow> Rows, int Total)> GetDailyRowsPagedAsync(
        DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct = default);
    Task<IList<Sale>> GetByShiftAsync(Guid shiftId, CancellationToken ct = default);
    Task<int> GetMaxReceiptSequenceAsync(string prefix, CancellationToken ct = default);
    Task AddAsync(Sale sale, CancellationToken ct = default);
    Task UpdateAsync(Sale sale, CancellationToken ct = default);
}
