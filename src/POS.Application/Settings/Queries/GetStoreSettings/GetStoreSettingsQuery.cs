using MediatR;

namespace POS.Application.Settings.Queries.GetStoreSettings;

public record GetStoreSettingsQuery : IRequest<StoreSettingsDto>;

public record StoreSettingsDto(string StoreName, string Address, string ReceiptFooter);
