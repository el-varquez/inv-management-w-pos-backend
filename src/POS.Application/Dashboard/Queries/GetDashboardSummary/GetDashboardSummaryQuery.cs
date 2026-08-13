using MediatR;

namespace POS.Application.Dashboard.Queries.GetDashboardSummary;

public record GetDashboardSummaryQuery : IRequest<DashboardSummaryDto>;

public record TodayKpiDto(
    decimal PaidSales,
    int TransactionCount,
    decimal AverageSale,
    decimal YesterdayPaidSales,
    decimal? DeltaPercent
);

public record StockHealthDto(int TotalItems, int InStock, int LowStock, int OutOfStock);

public record TopUtangRowDto(
    Guid SukiId, string Name, decimal Balance, int ChargeCount, int OldestDays);

public record UtangSnapshotDto(
    decimal TotalOutstanding,
    int SukiCount,
    decimal CollectedThisWeek,
    IList<TopUtangRowDto> Top
);

public record PaymentSplitRowDto(string Method, decimal Amount, int TransactionCount);

public record RunningOutRowDto(Guid ItemId, string Name, int Stock, int LowStockThreshold);

public record DashboardSummaryDto(
    TodayKpiDto Today,
    StockHealthDto StockHealth,
    UtangSnapshotDto Utang,
    IList<PaymentSplitRowDto> PaymentsToday,
    IList<RunningOutRowDto> RunningOut
);
