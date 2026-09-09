using MediatR;
using POS.Application.Common;
using POS.Domain.Events;
using POS.Domain.Interfaces;

namespace POS.Application.Invoices.EventHandlers;

public class InvoiceCreatedEventHandler : INotificationHandler<InvoiceCreatedEvent>
{
    private readonly IItemRepository _items;
    private readonly ICompositeItemRepository _composites;
    private readonly IStockMovementRepository _stockMovements;

    public InvoiceCreatedEventHandler(
        IItemRepository items,
        ICompositeItemRepository composites,
        IStockMovementRepository stockMovements)
    {
        _items = items;
        _composites = composites;
        _stockMovements = stockMovements;
    }

    public Task Handle(InvoiceCreatedEvent notification, CancellationToken ct)
        => StockLedger.DeductAsync(
            _items, _composites, _stockMovements,
            notification.SoldItems, notification.CreatedBy, ct);
}
