using MediatR;
using POS.Application.Common.Models;

namespace POS.Application.Utang.Queries.GetSukiLedgerEntries;

/// <summary>
/// A page of a suki's ledger. <see cref="From"/> and <see cref="To"/> (inclusive) bound the period and
/// <see cref="Search"/> matches invoice number, note or exact amount. All three select rows after the
/// running balance is computed over the whole ledger, so a filtered row keeps its true balance.
/// </summary>
public record GetSukiLedgerEntriesQuery(
    Guid SukiId,
    int? Page,
    int? PageSize,
    DateTime? From = null,
    DateTime? To = null,
    string? Search = null
) : IRequest<LedgerEntriesPageDto>;

/// <summary>
/// Paged ledger entries plus the balance carried into the period: <see cref="OpeningBalance"/> is the
/// running balance after the last non-voided entry dated before <c>From</c>, or 0 without a <c>From</c>.
/// </summary>
public record LedgerEntriesPageDto(
    IReadOnlyList<UtangLedgerEntryDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    decimal OpeningBalance
) : PagedResult<UtangLedgerEntryDto>(Items, Page, PageSize, TotalCount);

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
