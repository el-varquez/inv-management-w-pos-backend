using MediatR;
using POS.Application.Common.Models;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Utang.Queries.GetSukiLedgerEntries;

public class GetSukiLedgerEntriesQueryHandler
    : IRequestHandler<GetSukiLedgerEntriesQuery, PagedResult<UtangLedgerEntryDto>>
{
    private readonly IUtangRepository _utang;

    public GetSukiLedgerEntriesQueryHandler(IUtangRepository utang) => _utang = utang;

    public async Task<PagedResult<UtangLedgerEntryDto>> Handle(
        GetSukiLedgerEntriesQuery request, CancellationToken ct)
    {
        _ = await _utang.GetSukiByIdAsync(request.SukiId, ct)
            ?? throw new NotFoundException("Suki", request.SukiId);

        var (page, pageSize) = Pagination.Normalize(request.Page, request.PageSize);
        var (rows, total) = await _utang.GetLedgerEntriesPagedAsync(request.SukiId, page, pageSize, ct);

        var items = rows
            .Select(r => new UtangLedgerEntryDto(
                r.Id,
                r.Type,
                r.Amount,
                r.Markup,
                r.InvoiceId,
                r.InvoiceNumber,
                r.Type == "Payment" ? r.Note ?? "Payment received" : r.Note,
                r.IsVoided,
                r.EditedFrom,
                r.CreatedAt,
                r.RunningBalance))
            .ToList();

        return new PagedResult<UtangLedgerEntryDto>(items, page, pageSize, total);
    }
}
