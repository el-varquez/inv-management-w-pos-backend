using MediatR;

namespace POS.Application.Items.Commands.CreateItem;

public record CreateItemCommand(
    string Name,
    string? Description,
    string? ItemCode,
    string? Barcode,
    decimal CostPrice,
    decimal SellingPrice,
    int LowStockThreshold,
    Guid CategoryId
) : IRequest<Guid>;