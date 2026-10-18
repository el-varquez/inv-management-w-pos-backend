using POS.Domain.Entities;

namespace POS.Domain.Interfaces;

public record SukiWithBalance(
    Suki Suki,
    decimal Balance,
    int ChargeCount,
    DateTime? OldestChargeAt,
    IReadOnlyList<Invoice> Invoices,
    IReadOnlyList<Payment> Payments);

/// <summary>
/// One ledger line for a suki. <see cref="RunningBalance"/> is the balance after this entry in
/// chronological order; it is null on voided entries, which also do not move it.
/// </summary>
public record LedgerEntryRow(
    Guid Id,
    string Type,
    decimal Amount,
    decimal Markup,
    Guid? InvoiceId,
    string? InvoiceNumber,
    string? Note,
    bool IsVoided,
    decimal? EditedFrom,
    DateTime CreatedAt,
    decimal? RunningBalance);

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

    /// <summary>The suki's charges and payments merged, newest first, each carrying its running balance.</summary>
    Task<(IList<LedgerEntryRow> Items, int Total)> GetLedgerEntriesPagedAsync(
        Guid sukiId, int page, int pageSize, CancellationToken ct = default);
}
