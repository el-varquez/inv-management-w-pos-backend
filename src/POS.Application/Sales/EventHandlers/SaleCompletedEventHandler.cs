using MediatR;
using POS.Application.Common.Interfaces;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Domain.Events;
using POS.Domain.Interfaces;

namespace POS.Application.Sales.EventHandlers;

public class SaleCompletedEventHandler : INotificationHandler<SaleCompletedEvent>
{
    private readonly IItemRepository _itemRepository;
    private readonly ICompositeItemRepository _compositeItemRepository;
    private readonly IStockMovementRepository _stockMovementRepository;

    public SaleCompletedEventHandler(
        IItemRepository itemRepository,
        ICompositeItemRepository compositeItemRepository,
        IStockMovementRepository stockMovementRepository)
    {
        _itemRepository = itemRepository;
        _compositeItemRepository = compositeItemRepository;
        _stockMovementRepository = stockMovementRepository;
    }

    public async Task Handle(SaleCompletedEvent notification, CancellationToken ct)
    {
        var movements = new List<StockMovement>();

        foreach (var (itemId, quantity) in notification.SoldItems)
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

                    var deductQty = (int)Math.Ceiling(component.Quantity * quantity);
                    componentItem.Stock -= deductQty;
                    componentItem.UpdatedAt = DateTime.UtcNow;
                    await _itemRepository.UpdateAsync(componentItem, ct);

                    movements.Add(new StockMovement
                    {
                        ItemId = componentItem.Id,
                        Type = StockMovementType.Sale,
                        Quantity = -deductQty,
                        Notes = $"Component of '{item.Name}' sale",
                        CreatedBy = notification.CreatedBy
                    });
                }
            }
            else
            {
                item.Stock -= quantity;
                item.UpdatedAt = DateTime.UtcNow;
                await _itemRepository.UpdateAsync(item, ct);

                movements.Add(new StockMovement
                {
                    ItemId = item.Id,
                    Type = StockMovementType.Sale,
                    Quantity = -quantity,
                    CreatedBy = notification.CreatedBy
                });
            }
        }

        if (movements.Any())
            await _stockMovementRepository.AddRangeAsync(movements, ct);
    }
}