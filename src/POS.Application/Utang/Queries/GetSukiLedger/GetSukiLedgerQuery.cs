using MediatR;

namespace POS.Application.Utang.Queries.GetSukiLedger;

public record GetSukiLedgerQuery(Guid SukiId) : IRequest<SukiLedgerDto>;

public record UtangLedgerEntryDto(
    Guid Id,
    string Type,
    decimal Amount,
    decimal Markup,
    Guid? TransactionId,
    string? ReceiptNumber,
    string? Note,
    bool IsVoided,
    decimal? EditedFrom,
    DateTime CreatedAt);

public record SukiLedgerDto(
    Guid Id,
    string Name,
    string? Phone,
    decimal Balance,
    decimal MarkupEarned,
    IList<UtangLedgerEntryDto> Entries);
