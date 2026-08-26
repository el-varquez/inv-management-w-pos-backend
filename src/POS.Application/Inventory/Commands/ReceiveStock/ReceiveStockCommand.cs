using MediatR;

namespace POS.Application.Inventory.Commands.ReceiveStock;

public record NewReceiveItem(
    string Name,
    string? Barcode
);

public record ReceiveStockLine(
    Guid? ItemId,
    int Quantity,
    decimal CostPerUnit,
    decimal SellingPrice,
    NewReceiveItem? NewItem = null
);

public record ReceiveStockCommand(
    string? SupplierName,
    string? Notes,
    IReadOnlyList<ReceiveStockLine> Lines
) : IRequest<int>;
