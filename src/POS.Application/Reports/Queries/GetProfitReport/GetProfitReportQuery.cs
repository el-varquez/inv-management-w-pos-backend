using MediatR;

namespace POS.Application.Reports.Queries.GetProfitReport;

public record GetProfitReportQuery(
    DateTime? From,
    DateTime? To,
    Guid? CategoryId,
    Guid? ItemId
) : IRequest<ProfitReportDto>;

public record ProfitDetailDto(
    Guid ItemId,
    string ItemName,
    string CategoryName,
    int QuantitySold,
    decimal Revenue,
    decimal Cost,
    decimal Profit,
    decimal MarginPercent
);

public record ProfitReportDto(
    decimal NetSales,
    decimal CostOfGoodsSold,
    decimal GrossProfit,
    decimal InventoryLoss,
    decimal NetProfit,
    decimal GrossMarginPercent,
    IList<ProfitDetailDto> Details,
    DateTime? From,
    DateTime? To
);
