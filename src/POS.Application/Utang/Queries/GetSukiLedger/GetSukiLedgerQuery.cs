using MediatR;

namespace POS.Application.Utang.Queries.GetSukiLedger;

public record GetSukiLedgerQuery(Guid SukiId) : IRequest<SukiLedgerDto>;

/// <summary>The ledger header. Entries are paged separately through GetSukiLedgerEntries.</summary>
public record SukiLedgerDto(
    Guid Id,
    string Name,
    string? Phone,
    decimal Balance,
    decimal AmountPaid,
    decimal MarkupEarned,
    DateTime? DebtSince,
    DateTime? LastPaidAt,
    int? DaysSincePayment,
    bool PaymentOverdue);
