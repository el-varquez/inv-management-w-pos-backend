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

    Task<IList<UtangCharge>> GetChargesBySukiAsync(
        Guid sukiId, CancellationToken ct = default);
    Task<IList<UtangCharge>> GetChargesByShiftAsync(
        Guid shiftId, CancellationToken ct = default);
    Task<IList<UtangCharge>> GetChargesByTransactionAsync(
        Guid transactionId, CancellationToken ct = default);
    Task<IList<UtangCharge>> GetChargesInRangeAsync(
        DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default);
    Task AddChargeAsync(UtangCharge charge, CancellationToken ct = default);

    Task<UtangPayment?> GetPaymentByIdAsync(Guid id, CancellationToken ct = default);
    Task<IList<UtangPayment>> GetPaymentsBySukiAsync(
        Guid sukiId, CancellationToken ct = default);
    Task<IList<UtangPayment>> GetPaymentsByShiftAsync(
        Guid shiftId, CancellationToken ct = default);
    Task<IList<UtangPayment>> GetPaymentsByTransactionAsync(
        Guid transactionId, CancellationToken ct = default);
    Task<IList<UtangPayment>> GetPaymentsSinceAsync(
        DateTime fromUtc, CancellationToken ct = default);
    Task<IList<UtangPayment>> GetPaymentsInRangeAsync(
        DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default);
    Task AddPaymentAsync(UtangPayment payment, CancellationToken ct = default);

    Task<decimal> GetBalanceAsync(Guid sukiId, CancellationToken ct = default);
}
