using MediatR;
using POS.Domain.Interfaces;

namespace POS.Application.Reports.Queries.GetSalesReport;

public class GetSalesReportQueryHandler
    : IRequestHandler<GetSalesReportQuery, SalesReportDto>
{
    private readonly ITransactionRepository _transactionRepository;

    public GetSalesReportQueryHandler(ITransactionRepository transactionRepository)
        => _transactionRepository = transactionRepository;

    public async Task<SalesReportDto> Handle(
        GetSalesReportQuery request, CancellationToken ct)
    {
        var transactions = await _transactionRepository.GetAllAsync(
            request.From, request.To, ct);

        var sales = transactions.Where(t => t.RefundedFromId == null).ToList();
        var refundTxns = transactions.Where(t => t.RefundedFromId != null).ToList();

        var dailyBreakdown = transactions
            .GroupBy(t => t.CreatedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var daySales = g.Where(t => t.RefundedFromId == null).ToList();
                var dayRefunds = g.Where(t => t.RefundedFromId != null).ToList();

                var gross = daySales.Sum(t => t.Subtotal);
                var discounts = daySales.Sum(t => t.DiscountAmount);
                var refunds = Math.Abs(dayRefunds.Sum(t => t.Total));
                var net = daySales.Sum(t => t.Total) - refunds;

                return new SalesReportDailyDto(
                    g.Key, gross, discounts, refunds, net, daySales.Count);
            })
            .ToList();

        var totalGross = sales.Sum(t => t.Subtotal);
        var totalDiscounts = sales.Sum(t => t.DiscountAmount);
        var totalRefunds = Math.Abs(refundTxns.Sum(t => t.Total));
        var netSales = sales.Sum(t => t.Total) - totalRefunds;

        return new SalesReportDto(
            totalGross,
            totalDiscounts,
            totalRefunds,
            netSales,
            sales.Count,
            dailyBreakdown,
            request.From,
            request.To
        );
    }
}
