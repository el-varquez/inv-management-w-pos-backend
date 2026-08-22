using MediatR;
using POS.Domain.Interfaces;

namespace POS.Application.Utang.Queries.GetUtangSummary;

public class GetUtangSummaryQueryHandler
    : IRequestHandler<GetUtangSummaryQuery, UtangSummaryDto>
{
    private readonly IUtangRepository _utang;

    public GetUtangSummaryQueryHandler(IUtangRepository utang) => _utang = utang;

    public async Task<UtangSummaryDto> Handle(
        GetUtangSummaryQuery request, CancellationToken ct)
    {
        var charges = await _utang.GetChargesInRangeAsync(request.From, request.To, ct);
        var payments = await _utang.GetPaymentsInRangeAsync(request.From, request.To, ct);
        var liveCharges = charges.Where(c => !c.IsVoided).ToList();

        var top = liveCharges
            .GroupBy(c => c.SukiId)
            .Select(g => new
            {
                g.First().Suki.Name,
                Charged = g.Sum(c => c.Amount)
            })
            .OrderByDescending(x => x.Charged)
            .ThenBy(x => x.Name)
            .FirstOrDefault();

        return new UtangSummaryDto(
            liveCharges.Sum(c => c.Amount),
            payments.Where(p => !p.IsVoided).Sum(p => p.Amount),
            top?.Name,
            top?.Charged ?? 0m);
    }
}
