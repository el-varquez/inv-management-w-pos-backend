using Microsoft.EntityFrameworkCore;
using POS.Domain.Entities;
using POS.Domain.Interfaces;

namespace POS.Infrastructure.Persistence.Repositories;

public class StoreSettingsRepository : IStoreSettingsRepository
{
    private readonly AppDbContext _ctx;
    public StoreSettingsRepository(AppDbContext ctx) => _ctx = ctx;

    public Task<StoreSettings?> GetAsync(CancellationToken ct = default)
        => _ctx.StoreSettings.SingleOrDefaultAsync(ct);

    public async Task AddAsync(StoreSettings settings, CancellationToken ct = default)
        => await _ctx.StoreSettings.AddAsync(settings, ct);
}
