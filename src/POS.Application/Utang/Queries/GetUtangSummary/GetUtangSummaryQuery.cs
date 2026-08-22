using MediatR;

namespace POS.Application.Utang.Queries.GetUtangSummary;

public record GetUtangSummaryQuery(
    DateTime? From,
    DateTime? To
) : IRequest<UtangSummaryDto>;

public record UtangSummaryDto(
    decimal TotalCharged,
    decimal TotalPaid,
    string? TopSukiName,
    decimal TopSukiCharged
);
