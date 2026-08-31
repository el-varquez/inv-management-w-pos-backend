using POS.Domain.Entities;

namespace POS.Domain.Interfaces;

public interface IPaymentMethodRepository
{
    Task<IList<PaymentMethod>> GetAllAsync(CancellationToken ct = default);
    Task<PaymentMethod?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PaymentMethod?> GetByNameAsync(string name, CancellationToken ct = default);
    Task AddAsync(PaymentMethod method, CancellationToken ct = default);
}
