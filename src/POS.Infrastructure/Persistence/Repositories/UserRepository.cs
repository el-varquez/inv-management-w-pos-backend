using Microsoft.EntityFrameworkCore;
using POS.Domain.Entities;
using POS.Domain.Interfaces;

namespace POS.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context) => _context = context;

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await _context.Users.FirstOrDefaultAsync(u => u.Email == email.ToLower(), ct);
    
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Users.FindAsync(new object[] { id }, ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
        => await _context.Users.AddAsync(user, ct);

    public async Task<IReadOnlyList<User>> GetCashiersByTenantAsync(Guid tenantId, CancellationToken ct = default)
        => await _context.Users
            .Where(u => u.TenantId == tenantId && u.Role == "Cashier")
            .OrderBy(u => u.Name)
            .ToListAsync(ct);

    public async Task<int> CountActiveCashiersAsync(Guid tenantId, CancellationToken ct = default)
        => await _context.Users
            .CountAsync(u => u.TenantId == tenantId && u.Role == "Cashier" && u.IsActive, ct);

    public async Task<User?> GetCashierByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
        => await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId && u.Role == "Cashier", ct);
}