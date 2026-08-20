using MediatR;

namespace POS.Application.Items.Queries.GetPopularItems;

public record GetPopularItemsQuery : IRequest<IList<PopularItemDto>>;

public record PopularItemDto(
    Guid Id,
    string Name,
    string? Barcode,
    string ItemCode,
    decimal Price,
    int Stock,
    bool IsComposite,
    bool TracksStock,
    int QuantitySold);
