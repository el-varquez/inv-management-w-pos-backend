using MediatR;
using POS.Domain.Interfaces;

namespace POS.Application.Utang.Queries.GetUtangOutstanding;

public class GetUtangOutstandingQueryHandler
    : IRequestHandler<GetUtangOutstandingQuery, UtangOutstandingDto>
{
    private readonly IUtangRepository _utang;

    public GetUtangOutstandingQueryHandler(IUtangRepository utang) => _utang = utang;

    public async Task<UtangOutstandingDto> Handle(
        GetUtangOutstandingQuery request, CancellationToken ct)
    {
        var owing = (await _utang.GetAllSukiBalancesAsync(ct))
            .Where(b => b.Balance > 0m)
            .ToList();
        return new UtangOutstandingDto(owing.Sum(b => b.Balance), owing.Count);
    }
}
