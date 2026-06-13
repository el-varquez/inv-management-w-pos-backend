using MediatR;

namespace POS.Application.Items.Commands.CreateItem;

public record CreateItemCommand(
    string Name,
    string? Description,
    string? Sku,
    decimal CostPrice,
    decimal SellingPrice,
    int LowStockThreshold,
    Guid CategoryId
) : IRequest<Guid>;