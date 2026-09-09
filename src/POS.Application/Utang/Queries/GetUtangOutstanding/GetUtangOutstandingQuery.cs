using MediatR;

namespace POS.Application.Utang.Queries.GetUtangOutstanding;

public record GetUtangOutstandingQuery : IRequest<UtangOutstandingDto>;

public record UtangOutstandingDto(decimal TotalOwed, int OwingCount);
