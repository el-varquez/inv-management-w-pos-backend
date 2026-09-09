using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Common;

public static class UtangGate
{
    public const string OffMessage = "Utang is turned off — turn it on in web admin Settings.";

    public static async Task RequireOnAsync(
        IStoreSettingsRepository settings, CancellationToken ct)
    {
        var current = await settings.GetAsync(ct);
        if (current is null || !current.AcceptUtang)
            throw new DomainException(OffMessage);
    }
}
