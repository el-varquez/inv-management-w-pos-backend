using MediatR;
using POS.Application.Common;
using POS.Domain.Interfaces;

namespace POS.Application.Dashboard.Queries.GetSalesTrend;

public class GetSalesTrendQueryHandler
    : IRequestHandler<GetSalesTrendQuery, SalesTrendDto>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUtangRepository _utangRepository;

    public GetSalesTrendQueryHandler(
        ITransactionRepository transactionRepository,
        IUtangRepository utangRepository)
    {
        _transactionRepository = transactionRepository;
        _utangRepository = utangRepository;
    }

    public async Task<SalesTrendDto> Handle(GetSalesTrendQuery request, CancellationToken ct)
    {
        var period = request.Period.ToLowerInvariant();
        var todayLocal = DateTime.Today;

        var (fromLocal, bucketCount) = period switch
        {
            "day" => (todayLocal, 24),
            "week" => (todayLocal.AddDays(-6), 7),
            "month" => (todayLocal.AddDays(-29), 30),
            _ => (new DateTime(todayLocal.Year, todayLocal.Month, 1).AddMonths(-11), 12),
        };

        var transactions = await _transactionRepository.GetAllAsync(
            fromLocal.ToUniversalTime(), null, ct);
        var charges = (await _utangRepository.GetChargesInRangeAsync(
            fromLocal.ToUniversalTime(), null, ct))
            .Where(c => !c.IsVoided)
            .ToList();

        var buckets = new List<SalesTrendBucketDto>(bucketCount);
        for (var i = 0; i < bucketCount; i++)
        {
            var startLocal = period switch
            {
                "day" => fromLocal.AddHours(i),
                "week" or "month" => fromLocal.AddDays(i),
                _ => fromLocal.AddMonths(i),
            };
            var endLocal = period switch
            {
                "day" => startLocal.AddHours(1),
                "week" or "month" => startLocal.AddDays(1),
                _ => startLocal.AddMonths(1),
            };

            var inBucket = transactions
                .Where(t => t.CreatedAt.ToLocalTime() >= startLocal
                         && t.CreatedAt.ToLocalTime() < endLocal)
                .ToList();

            var chargedInBucket = charges
                .Where(c => c.CreatedAt.ToLocalTime() >= startLocal
                         && c.CreatedAt.ToLocalTime() < endLocal)
                .Sum(c => c.Amount);

            buckets.Add(new SalesTrendBucketDto(
                startLocal, PaidSales.Net(inBucket), chargedInBucket));
        }

        return new SalesTrendDto(
            period,
            buckets,
            buckets.Sum(b => b.PaidSales),
            buckets.Sum(b => b.UtangCharged));
    }
}
