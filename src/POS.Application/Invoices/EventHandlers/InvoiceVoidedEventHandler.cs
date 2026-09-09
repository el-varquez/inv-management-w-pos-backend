using MediatR;
using POS.Application.Common;
using POS.Domain.Events;
using POS.Domain.Interfaces;

namespace POS.Application.Invoices.EventHandlers;

public class InvoiceVoidedEventHandler : INotificationHandler<InvoiceVoidedEvent>
{
    private readonly IItemRepository _items;
    private readonly ICompositeItemRepository _composites;
    private readonly IStockMovementRepository _stockMovements;

    public InvoiceVoidedEventHandler(
        IItemRepository items,
        ICompositeItemRepository composites,
        IStockMovementRepository stockMovements)
    {
        _items = items;
        _composites = composites;
        _stockMovements = stockMovements;
    }

    public Task Handle(InvoiceVoidedEvent notification, CancellationToken ct)
        => StockLedger.RestoreAsync(
            _items, _composites, _stockMovements,
            notification.RestockedItems, notification.CreatedBy, ct);
}
