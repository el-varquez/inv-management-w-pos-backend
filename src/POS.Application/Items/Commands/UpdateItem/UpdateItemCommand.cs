using MediatR;

namespace POS.Application.Items.Commands.UpdateItem;

public record UpdateItemCommand(
    Guid Id,
    string Name,
    string? Description,
    string? ItemCode,
    string? Barcode,
    decimal CostPrice,
    decimal SellingPrice,
    int LowStockThreshold,
    Guid CategoryId,
    bool IsActive,
    decimal? UtangMarkup,
    bool TracksStock
) : IRequest;