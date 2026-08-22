using MediatR;
using POS.Domain.Enums;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Utang.Queries.GetSukiLedger;

public class GetSukiLedgerQueryHandler
    : IRequestHandler<GetSukiLedgerQuery, SukiLedgerDto>
{
    private readonly IUtangRepository _utang;

    public GetSukiLedgerQueryHandler(IUtangRepository utang) => _utang = utang;

    public async Task<SukiLedgerDto> Handle(
        GetSukiLedgerQuery request, CancellationToken ct)
    {
        var suki = await _utang.GetSukiByIdAsync(request.SukiId, ct)
            ?? throw new NotFoundException("Suki", request.SukiId);

        var entries = await _utang.GetEntriesBySukiAsync(suki.Id, ct);
        var live = entries.Where(e => !e.IsVoided).ToList();

        return new SukiLedgerDto(
            suki.Id,
            suki.Name,
            suki.Phone,
            live.Sum(e => e.Type == UtangEntryType.Charge ? e.Amount : -e.Amount),
            live.Where(e => e.Type == UtangEntryType.Charge).Sum(e => e.Markup),
            entries.Select(e => new UtangLedgerEntryDto(
                e.Id,
                e.Type.ToString(),
                e.Amount,
                e.Markup,
                e.TransactionId,
                e.Transaction?.ReceiptNumber,
                e.Note,
                e.IsVoided,
                e.EditedFrom,
                e.CreatedAt)).ToList());
    }
}
