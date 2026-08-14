using MediatR;
using POS.Domain.Interfaces;

namespace POS.Application.Settings.Queries.GetStoreSettings;

public class GetStoreSettingsQueryHandler : IRequestHandler<GetStoreSettingsQuery, StoreSettingsDto>
{
    private readonly IStoreSettingsRepository _settings;
    public GetStoreSettingsQueryHandler(IStoreSettingsRepository settings) => _settings = settings;

    public async Task<StoreSettingsDto> Handle(GetStoreSettingsQuery request, CancellationToken ct)
    {
        var settings = await _settings.GetAsync(ct);
        return settings is null
            ? new StoreSettingsDto("My Store", string.Empty, string.Empty, true, 0m)
            : new StoreSettingsDto(
                settings.StoreName,
                settings.Address,
                settings.ReceiptFooter,
                settings.AcceptUtang,
                settings.DefaultUtangMarkup);
    }
}
