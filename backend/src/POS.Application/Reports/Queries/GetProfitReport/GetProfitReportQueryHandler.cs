using MediatR;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Domain.Interfaces;

namespace POS.Application.Reports.Queries.GetProfitReport;

public class GetProfitReportQueryHandler
    : IRequestHandler<GetProfitReportQuery, ProfitReportDto>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IStockMovementRepository _stockMovementRepository;

    public GetProfitReportQueryHandler(
        ITransactionRepository transactionRepository,
        IStockMovementRepository stockMovementRepository)
    {
        _transactionRepository = transactionRepository;
        _stockMovementRepository = stockMovementRepository;
    }

    public async Task<ProfitReportDto> Handle(
        GetProfitReportQuery request, CancellationToken ct)
    {
        var transactions = await _transactionRepository.GetAllWithItemCategoriesAsync(
            request.From, request.To, ct);

        var isFiltered = request.ItemId.HasValue || request.CategoryId.HasValue;

        var allLines = transactions.SelectMany(t => t.Items);
        var lines = LineFilter(allLines, request).ToList();

        var cogs = lines.Sum(i => i.CostPrice * i.Quantity);

        decimal netSales;
        if (isFiltered)
        {
            netSales = lines.Sum(i => i.Total);
        }
        else
        {
            var sales = transactions.Where(t => t.RefundedFromId == null);
            var refunds = Math.Abs(transactions
                .Where(t => t.RefundedFromId != null)
                .Sum(t => t.Total));
            netSales = sales.Sum(t => t.Total) - refunds;
        }

        var grossProfit = netSales - cogs;

        var movements = await _stockMovementRepository.GetAllAsync(
            request.From, request.To, null, ct);

        var inventoryLoss = MovementFilter(movements, request)
            .Where(m =>
                (m.Type == StockMovementType.Adjustment ||
                 m.Type == StockMovementType.InventoryCount) &&
                m.Quantity < 0 &&
                (m.Reason == AdjustmentReason.Loss ||
                 m.Reason == AdjustmentReason.Damage ||
                 m.Reason == AdjustmentReason.Spoilage))
            .Sum(m => Math.Abs(m.Quantity) * m.Item.CostPrice);

        var netProfit = grossProfit - inventoryLoss;

        var grossMargin = netSales != 0
            ? Math.Round(grossProfit / netSales * 100, 2)
            : 0;

        var details = lines
            .GroupBy(i => i.ItemId)
            .Select(g =>
            {
                var revenue = g.Sum(i => i.Total);
                var cost = g.Sum(i => i.CostPrice * i.Quantity);
                var profit = revenue - cost;
                return new ProfitDetailDto(
                    g.Key,
                    g.Select(i => i.ItemName).FirstOrDefault() ?? string.Empty,
                    g.Select(i => i.Item?.Category?.Name).FirstOrDefault(n => n != null)
                        ?? "Uncategorized",
                    g.Sum(i => i.Quantity),
                    revenue,
                    cost,
                    profit,
                    revenue != 0 ? Math.Round(profit / revenue * 100, 2) : 0
                );
            })
            .OrderByDescending(d => d.Profit)
            .ToList();

        return new ProfitReportDto(
            netSales,
            cogs,
            grossProfit,
            inventoryLoss,
            netProfit,
            grossMargin,
            details,
            request.From,
            request.To
        );
    }

    private static IEnumerable<TransactionItem> LineFilter(
        IEnumerable<TransactionItem> lines, GetProfitReportQuery request)
    {
        if (request.ItemId.HasValue)
            return lines.Where(i => i.ItemId == request.ItemId.Value);
        if (request.CategoryId.HasValue)
            return lines.Where(i => i.Item != null && i.Item.CategoryId == request.CategoryId.Value);
        return lines;
    }

    private static IEnumerable<StockMovement> MovementFilter(
        IEnumerable<StockMovement> movements, GetProfitReportQuery request)
    {
        if (request.ItemId.HasValue)
            return movements.Where(m => m.ItemId == request.ItemId.Value);
        if (request.CategoryId.HasValue)
            return movements.Where(m => m.Item != null && m.Item.CategoryId == request.CategoryId.Value);
        return movements;
    }
}
