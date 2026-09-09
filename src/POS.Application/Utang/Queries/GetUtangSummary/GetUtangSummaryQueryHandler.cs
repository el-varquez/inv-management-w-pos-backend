using MediatR;
using POS.Domain.Interfaces;

namespace POS.Application.Utang.Queries.GetUtangSummary;

public class GetUtangSummaryQueryHandler
    : IRequestHandler<GetUtangSummaryQuery, UtangSummaryDto>
{
    private readonly IUtangRepository _utang;
    private readonly IInvoiceRepository _invoices;

    public GetUtangSummaryQueryHandler(IUtangRepository utang, IInvoiceRepository invoices)
    {
        _utang = utang;
        _invoices = invoices;
    }

    public async Task<UtangSummaryDto> Handle(
        GetUtangSummaryQuery request, CancellationToken ct)
    {
        var invoices = (await _invoices.GetAllAsync(request.From, request.To, ct))
            .Where(i => !i.IsVoided)
            .ToList();
        var payments = await _utang.GetPaymentsInRangeAsync(request.From, request.To, ct);

        var top = invoices
            .GroupBy(i => i.SukiId)
            .Select(g => new { g.First().Suki.Name, Charged = g.Sum(i => i.Total) })
            .OrderByDescending(x => x.Charged)
            .ThenBy(x => x.Name)
            .FirstOrDefault();

        return new UtangSummaryDto(
            invoices.Sum(i => i.Total),
            payments.Where(p => !p.IsVoided).Sum(p => p.Amount),
            top?.Name,
            top?.Charged ?? 0m);
    }
}
