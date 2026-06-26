using MediatR;

namespace POS.Application.Inventory.Commands.AddStock;

public record AddStockCommand(
    Guid ItemId,
    int Quantity,
    decimal CostPerUnit,
    string? SupplierName,
    string? Notes
) : IRequest<Guid>;