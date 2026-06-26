using MediatR;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Platform.Queries.GetTenant;

public class GetTenantQueryHandler : IRequestHandler<GetTenantQuery, TenantDetailDto>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;

    public GetTenantQueryHandler(
        ITenantRepository tenantRepository,
        IUserRepository userRepository)
    {
        _tenantRepository = tenantRepository;
        _userRepository = userRepository;
    }

    public async Task<TenantDetailDto> Handle(GetTenantQuery request, CancellationToken ct)
    {
        var tenant = await _tenantRepository.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Tenant", request.Id);

        var users = await _userRepository.GetByTenantAsync(tenant.Id, ct);

        var userDtos = users
            .Select(u => new TenantUserDto(u.Id, u.Name, u.Email, u.Role, u.IsActive))
            .ToList();

        var activeCashiers = users.Count(u => u.Role == "Cashier" && u.IsActive);

        return new TenantDetailDto(
            tenant.Id,
            tenant.Name,
            tenant.CreatedAt,
            tenant.CashierCap,
            activeCashiers,
            tenant.IsActive,
            userDtos);
    }
}
