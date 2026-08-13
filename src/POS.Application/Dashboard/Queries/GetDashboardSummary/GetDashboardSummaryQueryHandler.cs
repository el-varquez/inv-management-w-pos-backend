using MediatR;
using POS.Application.Common;
using POS.Domain.Enums;
using POS.Domain.Interfaces;

namespace POS.Application.Dashboard.Queries.GetDashboardSummary;

public class GetDashboardSummaryQueryHandler
    : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IItemRepository _itemRepository;

    public GetDashboardSummaryQueryHandler(
        ITransactionRepository transactionRepository,
        IItemRepository itemRepository)
    {
        _transactionRepository = transactionRepository;
        _itemRepository = itemRepository;
    }

    public async Task<DashboardSummaryDto> Handle(
        GetDashboardSummaryQuery request, CancellationToken ct)
    {
        // Local store day; CreatedAt is stored UTC (spec: date/time convention).
        var todayStartUtc = DateTime.Today.ToUniversalTime();
        var yesterdayStartUtc = todayStartUtc.AddDays(-1);

        var transactions = await _transactionRepository.GetAllAsync(
            yesterdayStartUtc, null, ct);
        var today = transactions.Where(t => t.CreatedAt >= todayStartUtc).ToList();
        var yesterday = transactions.Where(t => t.CreatedAt < todayStartUtc).ToList();

        var net = PaidSales.Net(today);
        var count = PaidSales.Count(today);
        var yesterdayNet = PaidSales.Net(yesterday);
        decimal? delta = yesterdayNet > 0
            ? Math.Round((net - yesterdayNet) / yesterdayNet * 100, 1)
            : null;

        var todayKpi = new TodayKpiDto(
            net,
            count,
            count > 0 ? Math.Round(net / count, 2) : 0,
            yesterdayNet,
            delta);

        var paymentsToday = Enum.GetValues<PaymentType>()
            .Select(pt =>
            {
                var forMethod = today.Where(t => t.PaymentType == pt).ToList();
                return new PaymentSplitRowDto(
                    DisplayName(pt), PaidSales.Net(forMethod), PaidSales.Count(forMethod));
            })
            .ToList();

        var items = (await _itemRepository.GetAllAsync(ct))
            .Where(i => i.IsActive && !i.IsComposite)
            .ToList();

        var outOfStock = items.Count(i => i.Stock <= 0);
        var lowStock = items.Count(i => i.Stock > 0 && i.Stock <= i.LowStockThreshold);
        var stockHealth = new StockHealthDto(
            items.Count, items.Count - lowStock - outOfStock, lowStock, outOfStock);

        var runningOut = items
            .Where(i => i.Stock <= i.LowStockThreshold)
            .OrderBy(i => i.Stock)
            .ThenByDescending(i => i.LowStockThreshold - i.Stock)
            .Take(5)
            .Select(i => new RunningOutRowDto(i.Id, i.Name, i.Stock, i.LowStockThreshold))
            .ToList();

        // Utang domain arrives with the desktop register (Phase 3) — zero stub until then.
        var utang = new UtangSnapshotDto(0m, 0, 0m, new List<TopUtangRowDto>());

        return new DashboardSummaryDto(
            todayKpi, stockHealth, utang, paymentsToday, runningOut);
    }

    // Brand spelling for the payment-method DTO — the frontend renders this verbatim.
    private static string DisplayName(PaymentType pt) => pt switch
    {
        PaymentType.Gcash => "GCash",
        PaymentType.Cash => "Cash",
        PaymentType.Maya => "Maya",
        _ => pt.ToString(),
    };
}
