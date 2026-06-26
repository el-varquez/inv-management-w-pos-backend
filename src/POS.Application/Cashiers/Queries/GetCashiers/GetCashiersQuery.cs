using MediatR;

namespace POS.Application.Cashiers.Queries.GetCashiers;

public record GetCashiersQuery : IRequest<CashierListDto>;

public record CashierDto(
    Guid Id,
    string Name,
    string Email,
    bool IsActive,
    DateTime CreatedAt
);

public record CashierListDto(
    IReadOnlyList<CashierDto> Cashiers,
    int ActiveCount,
    int CashierCap
);
