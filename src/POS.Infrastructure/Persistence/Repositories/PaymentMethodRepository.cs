using Microsoft.EntityFrameworkCore;
using POS.Domain.Entities;
using POS.Domain.Interfaces;

namespace POS.Infrastructure.Persistence.Repositories;

public class PaymentMethodRepository : IPaymentMethodRepository
{
    private readonly AppDbContext _context;
    public PaymentMethodRepository(AppDbContext context) => _context = context;

    public async Task<IList<PaymentMethod>> GetAllAsync(CancellationToken ct = default)
        => await _context.PaymentMethods
            .OrderBy(m => m.CreatedAt).ThenBy(m => m.Name)
            .ToListAsync(ct);

    public async Task<PaymentMethod?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.PaymentMethods.FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<PaymentMethod?> GetByNameAsync(string name, CancellationToken ct = default)
        => await _context.PaymentMethods
            .FirstOrDefaultAsync(m => m.Name.ToLower() == name.ToLower(), ct);

    public async Task AddAsync(PaymentMethod method, CancellationToken ct = default)
        => await _context.PaymentMethods.AddAsync(method, ct);
}
