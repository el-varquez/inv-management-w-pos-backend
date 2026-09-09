using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Domain.Interfaces;

namespace POS.Application.Common;

public static class StockLedger
{
    public static async Task DeductAsync(
        IItemRepository items,
        ICompositeItemRepository composites,
        IStockMovementRepository stockMovements,
        IReadOnlyList<(Guid ItemId, int Quantity)> sold,
        Guid createdBy,
        CancellationToken ct)
    {
        var movements = new List<StockMovement>();

        foreach (var (itemId, quantity) in sold)
        {
            var item = await items.GetByIdAsync(itemId, ct);
            if (item is null || !item.TracksStock) continue;

            if (item.IsComposite)
            {
                foreach (var component in await composites.GetByParentIdAsync(item.Id, ct))
                {
                    var componentItem = await items.GetByIdAsync(component.ComponentItemId, ct);
                    if (componentItem is null) continue;

                    var deductQty = (int)Math.Ceiling(component.Quantity * quantity);
                    componentItem.Stock -= deductQty;
                    componentItem.UpdatedAt = DateTime.UtcNow;
                    await items.UpdateAsync(componentItem, ct);

                    movements.Add(new StockMovement
                    {
                        ItemId = componentItem.Id,
                        Type = StockMovementType.Sale,
                        Quantity = -deductQty,
                        Notes = $"Component of '{item.Name}' sale",
                        CreatedBy = createdBy
                    });
                }
            }
            else
            {
                item.Stock -= quantity;
                item.UpdatedAt = DateTime.UtcNow;
                await items.UpdateAsync(item, ct);

                movements.Add(new StockMovement
                {
                    ItemId = item.Id,
                    Type = StockMovementType.Sale,
                    Quantity = -quantity,
                    CreatedBy = createdBy
                });
            }
        }

        if (movements.Any())
            await stockMovements.AddRangeAsync(movements, ct);
    }

    public static async Task RestoreAsync(
        IItemRepository items,
        ICompositeItemRepository composites,
        IStockMovementRepository stockMovements,
        IReadOnlyList<(Guid ItemId, int Quantity)> returned,
        Guid createdBy,
        CancellationToken ct)
    {
        var movements = new List<StockMovement>();

        foreach (var (itemId, quantity) in returned)
        {
            var item = await items.GetByIdAsync(itemId, ct);
            if (item is null || !item.TracksStock) continue;

            if (item.IsComposite)
            {
                foreach (var component in await composites.GetByParentIdAsync(item.Id, ct))
                {
                    var componentItem = await items.GetByIdAsync(component.ComponentItemId, ct);
                    if (componentItem is null) continue;

                    var restoreQty = (int)Math.Ceiling(component.Quantity * quantity);
                    componentItem.Stock += restoreQty;
                    componentItem.UpdatedAt = DateTime.UtcNow;
                    await items.UpdateAsync(componentItem, ct);

                    movements.Add(new StockMovement
                    {
                        ItemId = componentItem.Id,
                        Type = StockMovementType.Return,
                        Quantity = restoreQty,
                        Notes = $"Refund — component of '{item.Name}'",
                        CreatedBy = createdBy
                    });
                }
            }
            else
            {
                item.Stock += quantity;
                item.UpdatedAt = DateTime.UtcNow;
                await items.UpdateAsync(item, ct);

                movements.Add(new StockMovement
                {
                    ItemId = item.Id,
                    Type = StockMovementType.Return,
                    Quantity = quantity,
                    Notes = "Refund — stock restored",
                    CreatedBy = createdBy
                });
            }
        }

        if (movements.Any())
            await stockMovements.AddRangeAsync(movements, ct);
    }
}
