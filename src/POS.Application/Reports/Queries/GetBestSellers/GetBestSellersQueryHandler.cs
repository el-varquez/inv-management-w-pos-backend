using MediatR;
using POS.Domain.Enums;
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

        var lines = transactions
            .SelectMany(t => t.Items.Select(i => (t.PaymentType, Line: i)));

        return lines
            .GroupBy(x => x.Line.ItemId)
            .Select(g =>
            {
                var paidLines = g
                    .Where(x => x.PaymentType != PaymentType.Utang)
                    .Select(x => x.Line)
                    .ToList();
                var revenue = paidLines.Sum(i => i.Total);
                var profit = paidLines.Sum(i => i.Total - i.CostPrice * i.Quantity);
                return new BestSellerDto(
                    g.Key,
                    g.Select(x => x.Line.ItemName).FirstOrDefault() ?? string.Empty,
                    g.Sum(x => x.Line.Quantity),
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
