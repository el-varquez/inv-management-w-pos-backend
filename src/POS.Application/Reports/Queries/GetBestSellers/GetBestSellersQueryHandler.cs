using MediatR;
using POS.Domain.Interfaces;

namespace POS.Application.Reports.Queries.GetBestSellers;

public class GetBestSellersQueryHandler
    : IRequestHandler<GetBestSellersQuery, IList<BestSellerDto>>
{
    private readonly ISaleRepository _sales;
    private readonly IInvoiceRepository _invoices;

    public GetBestSellersQueryHandler(ISaleRepository sales, IInvoiceRepository invoices)
    {
        _sales = sales;
        _invoices = invoices;
    }

    public async Task<IList<BestSellerDto>> Handle(
        GetBestSellersQuery request, CancellationToken ct)
    {
        var sales = await _sales.GetAllAsync(request.From, request.To, ct);
        var invoices = await _invoices.GetAllAsync(request.From, request.To, ct);

        var saleLines = sales.SelectMany(s => s.Items).ToList();
        var quantities = saleLines
            .Select(l => (l.ItemId, l.ItemName, l.Quantity))
            .Concat(invoices
                .Where(i => !i.IsVoided)
                .SelectMany(i => i.Items)
                .Select(l => (l.ItemId, l.ItemName, l.Quantity)));

        return quantities
            .GroupBy(x => x.ItemId)
            .Select(g =>
            {
                var paidLines = saleLines.Where(l => l.ItemId == g.Key).ToList();
                var revenue = paidLines.Sum(i => i.Total);
                var profit = paidLines.Sum(i => i.Total - i.CostPrice * i.Quantity);
                return new BestSellerDto(
                    g.Key,
                    g.Select(x => x.ItemName).FirstOrDefault() ?? string.Empty,
                    g.Sum(x => x.Quantity),
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
