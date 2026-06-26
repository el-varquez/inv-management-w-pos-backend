using POS.Domain.Entities;

namespace POS.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);

    Task<IReadOnlyList<User>> GetCashiersByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<int> CountActiveCashiersAsync(Guid tenantId, CancellationToken ct = default);
    Task<User?> GetCashierByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);

    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);
}