using MediatR;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Domain.Events;
using POS.Domain.Interfaces;

namespace POS.Application.Sales.EventHandlers;

public class SaleRefundedEventHandler : INotificationHandler<SaleRefundedEvent>
{
    private readonly IItemRepository _itemRepository;
    private readonly ICompositeItemRepository _compositeItemRepository;
    private readonly IStockMovementRepository _stockMovementRepository;

    public SaleRefundedEventHandler(
        IItemRepository itemRepository,
        ICompositeItemRepository compositeItemRepository,
        IStockMovementRepository stockMovementRepository)
    {
        _itemRepository = itemRepository;
        _compositeItemRepository = compositeItemRepository;
        _stockMovementRepository = stockMovementRepository;
    }

    public async Task Handle(SaleRefundedEvent notification, CancellationToken ct)
    {
        var movements = new List<StockMovement>();

        foreach (var (itemId, quantity) in notification.RefundedItems)
        {
            var item = await _itemRepository.GetByIdAsync(itemId, ct);
            if (item is null) continue;

            if (item.IsComposite)
            {
                var components = await _compositeItemRepository
                    .GetByParentIdAsync(item.Id, ct);

                foreach (var component in components)
                {
                    var componentItem = await _itemRepository
                        .GetByIdAsync(component.ComponentItemId, ct);
                    if (componentItem is null) continue;

                    var restoreQty = (int)Math.Ceiling(component.Quantity * quantity);
                    componentItem.Stock += restoreQty;
                    componentItem.UpdatedAt = DateTime.UtcNow;
                    await _itemRepository.UpdateAsync(componentItem, ct);

                    movements.Add(new StockMovement
                    {
                        ItemId = componentItem.Id,
                        Type = StockMovementType.Return,
                        Quantity = restoreQty,
                        Notes = $"Refund — component of '{item.Name}'",
                        CreatedBy = notification.CreatedBy
                    });
                }
            }
            else
            {
                item.Stock += quantity;
                item.UpdatedAt = DateTime.UtcNow;
                await _itemRepository.UpdateAsync(item, ct);

                movements.Add(new StockMovement
                {
                    ItemId = item.Id,
                    Type = StockMovementType.Return,
                    Quantity = quantity,
                    Notes = "Refund — stock restored",
                    CreatedBy = notification.CreatedBy
                });
            }
        }

        if (movements.Any())
            await _stockMovementRepository.AddRangeAsync(movements, ct);
    }
}