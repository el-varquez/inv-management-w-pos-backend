using MediatR;
using POS.Domain.Interfaces;

namespace POS.Application.Reports.Queries.GetBestSellers;

public class GetBestSellersQueryHandler
    : IRequestHandler<GetBestSellersQuery, IList<BestSellerDto>>
{
    private readonly ITransactionRepository _transactionRepository;

    public GetBestSellersQueryHandler(ITransactionRepository transactionRepository)
        => _transactionRepository = transactionRepository;

    public async Task<IList<BestSellerDto>> Handle(
        GetBestSellersQuery request, CancellationToken ct)
    {
        var transactions = await _transactionRepository.GetAllAsync(
            request.From, request.To, ct);

        var lines = transactions.SelectMany(t => t.Items);

        return lines
            .GroupBy(i => i.ItemId)
            .Select(g =>
            {
                var revenue = g.Sum(i => i.Total);
                var profit = g.Sum(i => i.Total - i.CostPrice * i.Quantity);
                return new BestSellerDto(
                    g.Key,
                    g.Select(i => i.ItemName).FirstOrDefault() ?? string.Empty,
                    g.Sum(i => i.Quantity),
                    revenue,
                    profit,
                    revenue != 0 ? Math.Round(profit / revenue * 100, 2) : 0
                );
            })
            .Where(b => b.QuantitySold > 0)
            .OrderByDescending(b => b.QuantitySold)
            .ThenByDescending(b => b.Revenue)
            .ToList();
    }
}
