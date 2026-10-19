using MediatR;
using POS.Application.Common.Models;

namespace POS.Application.Utang.Queries.GetSukiLedgerEntries;

public record GetSukiLedgerEntriesQuery(
    Guid SukiId,
    int? Page,
    int? PageSize
) : IRequest<PagedResult<UtangLedgerEntryDto>>;

/// <summary>
/// One ledger entry, newest first. <see cref="RunningBalance"/> is the balance after this entry in
/// chronological order and is null on voided entries.
/// </summary>
public record UtangLedgerEntryDto(
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
