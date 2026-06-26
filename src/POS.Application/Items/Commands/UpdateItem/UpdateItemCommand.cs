using MediatR;

namespace POS.Application.Items.Commands.UpdateItem;

public record UpdateItemCommand(
    Guid Id,
    string Name,
    string? Description,
    string? Sku,
    decimal CostPrice,
    decimal SellingPrice,
    int LowStockThreshold,
    Guid CategoryId,
    bool IsActive
) : IRequest;