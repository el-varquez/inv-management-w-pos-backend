using POS.Domain.Entities;

namespace POS.Domain.Interfaces;

public record SukiWithBalance(
    Suki Suki,
    decimal Balance,
    int ChargeCount,
    DateTime? OldestChargeAt,
    IReadOnlyList<Invoice> Invoices,
    IReadOnlyList<Payment> Payments);

public interface IUtangRepository
{
    Task<Suki?> GetSukiByIdAsync(Guid id, CancellationToken ct = default);
    Task<(IList<SukiWithBalance> Items, int Total)> GetSukisPagedAsync(
        string? term, int page, int pageSize, CancellationToken ct = default);
    Task<IList<SukiWithBalance>> GetAllSukiBalancesAsync(CancellationToken ct = default);
    Task AddSukiAsync(Suki suki, CancellationToken ct = default);
    Task<bool> HasLedgerHistoryAsync(Guid sukiId, CancellationToken ct = default);
    Task DeleteSukiAsync(Guid id, CancellationToken ct = default);

    Task<Payment?> GetPaymentByIdAsync(Guid id, CancellationToken ct = default);
    Task<IList<Payment>> GetPaymentsBySukiAsync(Guid sukiId, CancellationToken ct = default);
    Task<IList<Payment>> GetPaymentsSinceAsync(DateTime fromUtc, CancellationToken ct = default);
    Task<IList<Payment>> GetPaymentsInRangeAsync(
        DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default);
    Task AddPaymentAsync(Payment payment, CancellationToken ct = default);

    Task<decimal> GetBalanceAsync(Guid sukiId, CancellationToken ct = default);
}
