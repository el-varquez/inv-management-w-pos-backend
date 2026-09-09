using POS.Domain.Entities;

namespace POS.Domain.Interfaces;

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
    Task<IList<Sale>> GetByShiftAsync(Guid shiftId, CancellationToken ct = default);
    Task<int> GetMaxReceiptSequenceAsync(string prefix, CancellationToken ct = default);
    Task AddAsync(Sale sale, CancellationToken ct = default);
    Task UpdateAsync(Sale sale, CancellationToken ct = default);
}