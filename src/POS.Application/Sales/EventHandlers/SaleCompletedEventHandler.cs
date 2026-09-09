using MediatR;
using POS.Application.Common;
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

    public Task Handle(SaleCompletedEvent notification, CancellationToken ct)
        => StockLedger.DeductAsync(
            _itemRepository, _compositeItemRepository, _stockMovementRepository,
            notification.SoldItems, notification.CreatedBy, ct);
}
