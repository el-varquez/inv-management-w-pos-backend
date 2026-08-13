using POS.Domain.Entities;

namespace POS.Domain.Interfaces;

public interface IStoreSettingsRepository
{
    Task<StoreSettings?> GetAsync(CancellationToken ct = default);
    Task AddAsync(StoreSettings settings, CancellationToken ct = default);
}
