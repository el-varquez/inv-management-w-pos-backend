using MediatR;
using POS.Application.Common;
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

    public Task Handle(SaleRefundedEvent notification, CancellationToken ct)
        => StockLedger.RestoreAsync(
            _itemRepository, _compositeItemRepository, _stockMovementRepository,
            notification.RefundedItems, notification.CreatedBy, ct);
}
