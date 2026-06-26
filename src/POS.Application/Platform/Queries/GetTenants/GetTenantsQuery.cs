using MediatR;

namespace POS.Application.Platform.Queries.GetTenants;

public record GetTenantsQuery : IRequest<IReadOnlyList<TenantSummaryDto>>;

public record TenantSummaryDto(
    Guid Id,
    string Name,
    DateTime CreatedAt,
    int UserCount,
    int ActiveCashierCount,
    int CashierCap,
    bool IsActive
);
