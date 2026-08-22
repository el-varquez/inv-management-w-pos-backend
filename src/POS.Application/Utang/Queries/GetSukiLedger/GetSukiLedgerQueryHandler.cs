using MediatR;
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

        var charges = await _utang.GetChargesBySukiAsync(suki.Id, ct);
        var payments = await _utang.GetPaymentsBySukiAsync(suki.Id, ct);
        var liveCharged = charges.Where(c => !c.IsVoided).Sum(c => c.Amount);
        var livePaid = payments.Where(p => !p.IsVoided).Sum(p => p.Amount);

        var entries = charges
            .Select(c => new UtangLedgerEntryDto(
                c.Id,
                "Charge",
                c.Amount,
                c.Markup,
                c.TransactionId,
                c.Transaction?.ReceiptNumber,
                null,
                c.IsVoided,
                null,
                c.CreatedAt))
            .Concat(payments.Select(p => new UtangLedgerEntryDto(
                p.Id,
                "Payment",
                p.Amount,
                0m,
                p.TransactionId,
                p.Transaction?.ReceiptNumber,
                p.TransactionId is null ? "Payment received" : "Down payment",
                p.IsVoided,
                p.EditedFrom,
                p.CreatedAt)))
            .OrderBy(e => e.CreatedAt)
            .ThenBy(e => e.Id)
            .ToList();

        return new SukiLedgerDto(
            suki.Id,
            suki.Name,
            suki.Phone,
            liveCharged - livePaid,
            charges.Where(c => !c.IsVoided).Sum(c => c.Markup),
            entries);
    }
}
