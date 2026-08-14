using MediatR;

namespace POS.Application.Items.Queries.SearchItems;

public record SearchItemsQuery(string Term, int? Limit = null)
    : IRequest<IList<SearchItemDto>>;

public record SearchItemDto(
    Guid Id,
    string Name,
    string? Barcode,
    string ItemCode,
    int Stock,
    decimal CostPrice,
    decimal SellingPrice,
    bool IsActive,
    bool IsComposite,
    string CategoryName
);
