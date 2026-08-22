using MediatR;
using POS.Domain.Enums;
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
        var entries = await _utang.GetEntriesInRangeAsync(
            request.From, request.To, ct);
        var live = entries.Where(e => !e.IsVoided).ToList();

        var top = live
            .Where(e => e.Type == UtangEntryType.Charge)
            .GroupBy(e => e.SukiId)
            .Select(g => new
            {
                g.First().Suki.Name,
                Charged = g.Sum(e => e.Amount)
            })
            .OrderByDescending(x => x.Charged)
            .ThenBy(x => x.Name)
            .FirstOrDefault();

        return new UtangSummaryDto(
            live.Where(e => e.Type == UtangEntryType.Charge).Sum(e => e.Amount),
            live.Where(e => e.Type == UtangEntryType.Payment).Sum(e => e.Amount),
            top?.Name,
            top?.Charged ?? 0m);
    }
}
