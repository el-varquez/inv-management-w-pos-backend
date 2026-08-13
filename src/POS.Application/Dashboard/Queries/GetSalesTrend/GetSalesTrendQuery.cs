using MediatR;

namespace POS.Application.Dashboard.Queries.GetSalesTrend;

public record GetSalesTrendQuery(string Period) : IRequest<SalesTrendDto>;

public record SalesTrendBucketDto(DateTime BucketStart, decimal PaidSales, decimal UtangCharged);

public record SalesTrendDto(
    string Period,
    IList<SalesTrendBucketDto> Buckets,
    decimal TotalPaidSales,
    decimal TotalUtangCharged
);
