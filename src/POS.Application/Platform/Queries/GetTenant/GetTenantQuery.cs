using MediatR;

namespace POS.Application.Platform.Queries.GetTenant;

public record GetTenantQuery(Guid Id) : IRequest<TenantDetailDto>;

public record TenantUserDto(
    Guid Id,
    string Name,
    string Email,
    string Role,
    bool IsActive
);

public record TenantDetailDto(
    Guid Id,
    string Name,
    DateTime CreatedAt,
    int CashierCap,
    int ActiveCashierCount,
    bool IsActive,
    IReadOnlyList<TenantUserDto> Users
);
