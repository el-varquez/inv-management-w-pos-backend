using MediatR;
using POS.Domain.Interfaces;

namespace POS.Application.Platform.Queries.GetTenants;

public class GetTenantsQueryHandler : IRequestHandler<GetTenantsQuery, IReadOnlyList<TenantSummaryDto>>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;

    public GetTenantsQueryHandler(
        ITenantRepository tenantRepository,
        IUserRepository userRepository)
    {
        _tenantRepository = tenantRepository;
        _userRepository = userRepository;
    }

    public async Task<IReadOnlyList<TenantSummaryDto>> Handle(GetTenantsQuery request, CancellationToken ct)
    {
        var tenants = await _tenantRepository.GetAllAsync(ct);
        var users = await _userRepository.GetAllAsync(ct);

        var byTenant = users
            .Where(u => u.TenantId is not null)
            .GroupBy(u => u.TenantId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        return tenants
            .Select(t =>
            {
                byTenant.TryGetValue(t.Id, out var tenantUsers);
                var list = tenantUsers ?? new();
                var activeCashiers = list.Count(u => u.Role == "Cashier" && u.IsActive);
                return new TenantSummaryDto(
                    t.Id,
                    t.Name,
                    t.CreatedAt,
                    list.Count,
                    activeCashiers,
                    t.CashierCap,
                    t.IsActive);
            })
            .ToList();
    }
}
