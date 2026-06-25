using MediatR;
using POS.Application.Common.Interfaces;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Cashiers.Queries.GetCashiers;

public class GetCashiersQueryHandler : IRequestHandler<GetCashiersQuery, CashierListDto>
{
    private readonly IUserRepository _userRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly ICurrentUser _currentUser;

    public GetCashiersQueryHandler(
        IUserRepository userRepository,
        ITenantRepository tenantRepository,
        ICurrentUser currentUser)
    {
        _userRepository = userRepository;
        _tenantRepository = tenantRepository;
        _currentUser = currentUser;
    }

    public async Task<CashierListDto> Handle(GetCashiersQuery request, CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId
            ?? throw new DomainException("No tenant context for the current user.");

        var tenant = await _tenantRepository.GetByIdAsync(tenantId, ct)
            ?? throw new NotFoundException("Tenant", tenantId);

        var cashiers = await _userRepository.GetCashiersByTenantAsync(tenantId, ct);

        var dtos = cashiers
            .Select(c => new CashierDto(c.Id, c.Name, c.Email, c.IsActive, c.CreatedAt))
            .ToList();

        var activeCount = dtos.Count(c => c.IsActive);

        return new CashierListDto(dtos, activeCount, tenant.CashierCap);
    }
}
