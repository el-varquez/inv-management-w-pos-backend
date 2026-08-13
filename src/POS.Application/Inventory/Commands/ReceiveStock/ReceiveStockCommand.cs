using MediatR;

namespace POS.Application.Inventory.Commands.ReceiveStock;

public record ReceiveStockLine(
    Guid ItemId,
    int Quantity,
    decimal CostPerUnit,
    decimal SellingPrice
);

public record ReceiveStockCommand(
    string? SupplierName,
    string? Notes,
    IReadOnlyList<ReceiveStockLine> Lines
) : IRequest<int>;
