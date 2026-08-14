using MediatR;

namespace POS.Application.Settings.Commands.UpdateStoreSettings;

public record UpdateStoreSettingsCommand(
    string StoreName,
    string Address,
    string ReceiptFooter,
    bool AcceptUtang,
    decimal DefaultUtangMarkup
) : IRequest;
