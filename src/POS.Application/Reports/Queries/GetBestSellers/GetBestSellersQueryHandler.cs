using MediatR;
using POS.Application.Common.Models;
using POS.Domain.Interfaces;

namespace POS.Application.Reports.Queries.GetBestSellers;

public class GetBestSellersQueryHandler
    : IRequestHandler<GetBestSellersQuery, PagedResult<BestSellerDto>>
{
    private readonly ISaleRepository _sales;
    private readonly IInvoiceRepository _invoices;

    public GetBestSellersQueryHandler(ISaleRepository sales, IInvoiceRepository invoices)
    {
        _sales = sales;
        _invoices = invoices;
    }

    public async Task<PagedResult<BestSellerDto>> Handle(
        GetBestSellersQuery request, CancellationToken ct)
    {
        var (page, pageSize) = Pagination.Normalize(request.Page, request.PageSize);

        // Both sources arrive already aggregated to one row per item, so the merge is
        // over the catalog's sold items, not over every line.
        var sold = await _sales.GetItemSalesRowsAsync(request.From, request.To, ct);
        var charged = await _invoices.GetItemQuantitiesAsync(request.From, request.To, ct);

        var soldById = sold.ToDictionary(r => r.ItemId);
        var chargedById = charged.ToDictionary(r => r.ItemId);

        var merged = soldById.Keys
            .Union(chargedById.Keys)
            .Select(itemId =>
            {
                soldById.TryGetValue(itemId, out var sale);
                chargedById.TryGetValue(itemId, out var invoice);

                var quantity = (sale?.Quantity ?? 0) + (invoice?.Quantity ?? 0);
                var revenue = sale?.Revenue ?? 0m;
                var profit = sale?.Profit ?? 0m;

                return new BestSellerDto(
                    itemId,
                    sale?.ItemName ?? invoice?.ItemName ?? string.Empty,
                    quantity,
                    revenue,
                    profit,
                    revenue != 0 ? Math.Round(profit / revenue * 100, 2) : 0
                );
            })
            .Where(b => b.QuantitySold > 0)
            .OrderByDescending(b => b.QuantitySold)
            .ThenByDescending(b => b.Revenue)
            .ThenBy(b => b.ItemId)
            .ToList();

        var items = merged
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<BestSellerDto>(items, page, pageSize, merged.Count);
    }
}
