using MediatR;
using POS.Application.Common.Models;
using POS.Application.Utang.Commands.CreateSuki;
using POS.Domain.Interfaces;

namespace POS.Application.Utang.Queries.GetSukis;

public class GetSukisQueryHandler
    : IRequestHandler<GetSukisQuery, PagedResult<SukiDto>>
{
    private readonly IUtangRepository _utang;

    public GetSukisQueryHandler(IUtangRepository utang) => _utang = utang;

    public async Task<PagedResult<SukiDto>> Handle(
        GetSukisQuery request, CancellationToken ct)
    {
        var (page, pageSize) = Pagination.Normalize(request.Page, request.PageSize);
        var (rows, total) = await _utang.GetSukisPagedAsync(
            request.Term, page, pageSize, ct);

        var dtos = rows
            .Select(r => new SukiDto(r.Suki.Id, r.Suki.Name, r.Suki.Phone, r.Balance))
            .ToList();

        return new PagedResult<SukiDto>(dtos, page, pageSize, total);
    }
}
