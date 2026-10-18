using MediatR;
using POS.Application.Common.Models;

namespace POS.Application.Reports.Queries.GetBestSellers;

public record GetBestSellersQuery(
    DateTime? From,
    DateTime? To,
    int? Page,
    int? PageSize
) : IRequest<PagedResult<BestSellerDto>>;

public record BestSellerDto(
    Guid ItemId,
    string ItemName,
    int QuantitySold,
    decimal Revenue,
    decimal Profit,
    decimal MarginPercent
);
