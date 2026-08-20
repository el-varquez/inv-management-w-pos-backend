using MediatR;

namespace POS.Application.Items.Queries.SearchSellableItems;

public record SearchSellableItemsQuery(string Term, int? Limit = null)
    : IRequest<IList<SellableItemDto>>;

public record SellableItemDto(
    Guid Id,
    string Name,
    string? Barcode,
    string ItemCode,
    decimal Price,
    int Stock,
    bool IsComposite,
    bool TracksStock);
