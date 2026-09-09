using MediatR;
using POS.Application.Common;
using POS.Application.Common.Interfaces;
using POS.Domain.Entities;
using POS.Domain.Events;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Invoices.Commands.CreateInvoice;

public class CreateInvoiceCommandHandler
    : IRequestHandler<CreateInvoiceCommand, CreateInvoiceResult>
{
    private readonly IItemRepository _items;
    private readonly ICompositeItemRepository _composites;
    private readonly IInvoiceRepository _invoices;
    private readonly IUtangRepository _utang;
    private readonly IShiftRepository _shifts;
    private readonly IStoreSettingsRepository _settings;
    private readonly IInvoiceNumberGenerator _numbers;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateInvoiceCommandHandler(
        IItemRepository items,
        ICompositeItemRepository composites,
        IInvoiceRepository invoices,
        IUtangRepository utang,
        IShiftRepository shifts,
        IStoreSettingsRepository settings,
        IInvoiceNumberGenerator numbers,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _items = items;
        _composites = composites;
        _invoices = invoices;
        _utang = utang;
        _shifts = shifts;
        _settings = settings;
        _numbers = numbers;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<CreateInvoiceResult> Handle(
        CreateInvoiceCommand request, CancellationToken ct)
    {
        var shift = await _shifts.GetOpenAsync(ct)
            ?? throw new DomainException(
                "No open shift — declare starting cash to start selling.");

        var settings = await _settings.GetAsync(ct);
        if (settings is null || !settings.AcceptUtang)
            throw new DomainException(UtangGate.OffMessage);

        var suki = await _utang.GetSukiByIdAsync(request.SukiId, ct)
            ?? throw new NotFoundException("Suki", request.SukiId);

        var lines = new List<InvoiceItem>();
        var soldItems = new List<(Guid ItemId, int Quantity)>();
        var demand = new Dictionary<Guid, int>();
        decimal subtotal = 0;
        decimal lineDiscounts = 0;
        decimal markupTotal = 0;

        foreach (var line in request.Items)
        {
            var item = await _items.GetByIdAsync(line.ItemId, ct)
                ?? throw new NotFoundException("Item", line.ItemId);

            decimal costPrice;
            if (item.IsComposite)
            {
                var components = await _composites.GetByParentIdAsync(item.Id, ct);
                foreach (var component in components)
                {
                    var required = (int)Math.Ceiling(component.Quantity * line.Quantity);
                    demand[component.ComponentItemId] =
                        demand.GetValueOrDefault(component.ComponentItemId) + required;
                }
                costPrice = components.Sum(c => c.Quantity * c.ComponentItem.CostPrice);
            }
            else
            {
                demand[item.Id] = demand.GetValueOrDefault(item.Id) + line.Quantity;
                costPrice = item.CostPrice;
            }

            var price = UtangPricing.Resolve(item, settings.DefaultUtangMarkup, line.Quantity);
            markupTotal += price.MarkupPerUnit * line.Quantity;
            var lineTotal = price.LineTotal - line.Discount;

            lines.Add(new InvoiceItem
            {
                ItemId = item.Id,
                ItemName = item.Name,
                UnitPrice = price.UnitPrice,
                CostPrice = costPrice,
                Quantity = line.Quantity,
                Discount = line.Discount,
                Total = lineTotal
            });

            soldItems.Add((item.Id, line.Quantity));
            subtotal += price.LineTotal;
            lineDiscounts += line.Discount;
        }

        foreach (var (itemId, required) in demand)
        {
            var stockItem = await _items.GetByIdAsync(itemId, ct);
            if (stockItem is null || !stockItem.TracksStock) continue;
            if (stockItem.Stock < required)
                throw new InsufficientStockException(stockItem.Name, required, stockItem.Stock);
        }

        var totalDiscount = lineDiscounts + request.InvoiceDiscount;
        var total = subtotal - totalDiscount;
        if (total < 0)
            throw new DomainException("Total cannot be negative after discounts.");

        var invoice = new Invoice
        {
            InvoiceNumber = await _numbers.GenerateAsync(ct),
            SukiId = suki.Id,
            ShiftId = shift.Id,
            Subtotal = subtotal,
            DiscountAmount = totalDiscount,
            MarkupTotal = markupTotal,
            Total = total,
            CreatedBy = _currentUser.Id,
            Items = lines
        };
        invoice.AddDomainEvent(
            new InvoiceCreatedEvent(invoice.Id, soldItems, _currentUser.Id));

        await _invoices.AddAsync(invoice, ct);
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await _unitOfWork.SaveChangesAsync(ct);
                break;
            }
            catch (InvoiceNumberCollisionException) when (attempt < 2)
            {
                invoice.InvoiceNumber = await _numbers.GenerateAsync(ct);
            }
        }

        var balance = await _utang.GetBalanceAsync(suki.Id, ct);
        return new CreateInvoiceResult(
            invoice.Id, invoice.InvoiceNumber,
            subtotal, totalDiscount, markupTotal, total, balance);
    }
}
