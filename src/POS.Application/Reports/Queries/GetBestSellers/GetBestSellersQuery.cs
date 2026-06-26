using MediatR;

namespace POS.Application.Reports.Queries.GetBestSellers;

public record GetBestSellersQuery(
    DateTime? From,
    DateTime? To
) : IRequest<IList<BestSellerDto>>;

public record BestSellerDto(
    Guid ItemId,
    string ItemName,
    int QuantitySold,
    decimal Revenue,
    decimal Profit,
    decimal MarginPercent
);
