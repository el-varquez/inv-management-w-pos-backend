using MediatR;
using POS.Domain.Interfaces;

namespace POS.Application.Sales.Queries.GetSalesSummary;

public class GetSalesSummaryQueryHandler
    : IRequestHandler<GetSalesSummaryQuery, SalesSummaryDto>
{
    private readonly ISaleRepository _sales;

    public GetSalesSummaryQueryHandler(ISaleRepository saleRepository)
        => _sales = saleRepository;

    public async Task<SalesSummaryDto> Handle(
        GetSalesSummaryQuery request, CancellationToken ct)
    {
        var allSales = await _sales.GetAllAsync(
            request.From, request.To, ct);

        var sales = allSales.Where(t => t.RefundedFromId == null).ToList();
        var refundTxns = allSales.Where(t => t.RefundedFromId != null).ToList();

        var grossSales = sales.Sum(t => t.Subtotal);
        var totalDiscounts = sales.Sum(t => t.DiscountAmount);
        var refunds = Math.Abs(refundTxns.Sum(t => t.Total));
        var netSales = sales.Sum(t => t.Total) - refunds;

        return new SalesSummaryDto(
            grossSales,
            totalDiscounts,
            refunds,
            netSales,
            sales.Count,
            request.From,
            request.To
        );
    }
}
