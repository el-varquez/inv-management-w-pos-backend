using POS.Domain.Entities;

namespace POS.Domain.Interfaces;

public record SukiWithBalance(
    Suki Suki, decimal Balance, int ChargeCount, DateTime? OldestChargeAt);

public interface IUtangRepository
{
    Task<Suki?> GetSukiByIdAsync(Guid id, CancellationToken ct = default);
    Task<(IList<SukiWithBalance> Items, int Total)> GetSukisPagedAsync(
        string? term, int page, int pageSize, CancellationToken ct = default);
    Task<IList<SukiWithBalance>> GetAllSukiBalancesAsync(CancellationToken ct = default);
    Task AddSukiAsync(Suki suki, CancellationToken ct = default);

    Task<UtangEntry?> GetEntryByIdAsync(Guid id, CancellationToken ct = default);
    Task<IList<UtangEntry>> GetEntriesBySukiAsync(
        Guid sukiId, CancellationToken ct = default);
    Task<IList<UtangEntry>> GetEntriesByShiftAsync(
        Guid shiftId, CancellationToken ct = default);
    Task<IList<UtangEntry>> GetEntriesByTransactionAsync(
        Guid transactionId, CancellationToken ct = default);
    Task<decimal> GetBalanceAsync(Guid sukiId, CancellationToken ct = default);
    Task<IList<UtangEntry>> GetPaymentsSinceAsync(
        DateTime fromUtc, CancellationToken ct = default);
    Task<IList<UtangEntry>> GetEntriesInRangeAsync(
        DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default);
    Task AddEntryAsync(UtangEntry entry, CancellationToken ct = default);
}
