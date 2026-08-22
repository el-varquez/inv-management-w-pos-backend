using MediatR;
using POS.Application.Common.Models;
using POS.Application.Utang.Commands.CreateSuki;

namespace POS.Application.Utang.Queries.GetSukis;

public record GetSukisQuery(string? Term, int? Page, int? PageSize)
    : IRequest<PagedResult<SukiDto>>;
