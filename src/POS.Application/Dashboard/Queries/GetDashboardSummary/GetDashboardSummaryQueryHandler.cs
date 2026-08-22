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
    private readonly IUtangRepository _utang;

    public GetDashboardSummaryQueryHandler(
        ITransactionRepository transactionRepository,
        IItemRepository itemRepository,
        IUtangRepository utang)
    {
        _transactionRepository = transactionRepository;
        _itemRepository = itemRepository;
        _utang = utang;
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
            .Where(pt => pt != PaymentType.Utang)
            .Select(pt =>
            {
                var forMethod = today.Where(t => t.PaymentType == pt).ToList();
                return new PaymentSplitRowDto(
                    DisplayName(pt), PaidSales.Net(forMethod), PaidSales.Count(forMethod));
            })
            .ToList();

        var items = (await _itemRepository.GetAllAsync(ct))
            .Where(i => i.IsActive && !i.IsComposite && i.TracksStock)
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

        var balances = await _utang.GetAllSukiBalancesAsync(ct);
        var owing = balances.Where(b => b.Balance > 0).ToList();
        var collected = (await _utang.GetPaymentsSinceAsync(
            DateTime.UtcNow.AddDays(-7), ct)).Sum(e => e.Amount);
        var utang = new UtangSnapshotDto(
            owing.Sum(b => b.Balance),
            owing.Count,
            collected,
            owing
                .OrderByDescending(b => b.Balance)
                .Take(5)
                .Select(b => new TopUtangRowDto(
                    b.Suki.Id,
                    b.Suki.Name,
                    b.Balance,
                    b.ChargeCount,
                    b.OldestChargeAt is { } oldest
                        ? (int)(DateTime.UtcNow - oldest).TotalDays
                        : 0))
                .ToList());

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
