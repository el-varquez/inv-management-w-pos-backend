using MediatR;
using POS.Domain.Enums;

namespace POS.Application.Inventory.Commands.AdjustStock;

public record AdjustStockCommand(
    Guid ItemId,
    int Quantity,          
    AdjustmentReason Reason,
    string? Notes
) : IRequest<Guid>;