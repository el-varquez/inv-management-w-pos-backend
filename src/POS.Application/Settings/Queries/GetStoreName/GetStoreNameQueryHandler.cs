using MediatR;
using POS.Domain.Interfaces;

namespace POS.Application.Settings.Queries.GetStoreName;

public class GetStoreNameQueryHandler : IRequestHandler<GetStoreNameQuery, StoreNameDto>
{
    private readonly IStoreSettingsRepository _settings;
    public GetStoreNameQueryHandler(IStoreSettingsRepository settings) => _settings = settings;

    public async Task<StoreNameDto> Handle(GetStoreNameQuery request, CancellationToken ct)
    {
        var settings = await _settings.GetAsync(ct);
        return new StoreNameDto(settings?.StoreName ?? "My Store");
    }
}
