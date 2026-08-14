using POS.Domain.Entities;

namespace POS.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);

    Task<IReadOnlyList<User>> GetCashiersAsync(CancellationToken ct = default);
    Task<User?> GetCashierByIdAsync(Guid id, CancellationToken ct = default);
}
