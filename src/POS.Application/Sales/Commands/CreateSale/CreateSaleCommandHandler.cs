using MediatR;
using POS.Application.Common.Interfaces;
using POS.Domain.Entities;
using POS.Domain.Events;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Sales.Commands.CreateSale;

public class CreateSaleCommandHandler
    : IRequestHandler<CreateSaleCommand, CreateSaleResult>
{
    private readonly IItemRepository _items;
    private readonly ISaleRepository _sales;
    private readonly IReceiptNumberGenerator _receiptGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ICompositeItemRepository _composites;
    private readonly IShiftRepository _shifts;
    private readonly IPaymentMethodRepository _paymentMethods;

    public CreateSaleCommandHandler(
        IItemRepository items,
        ISaleRepository sales,
        IReceiptNumberGenerator receiptGenerator,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ICompositeItemRepository composites,
        IShiftRepository shifts,
        IPaymentMethodRepository paymentMethods)
    {
        _items = items;
        _sales = sales;
        _receiptGenerator = receiptGenerator;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _composites = composites;
        _shifts = shifts;
        _paymentMethods = paymentMethods;
    }

    public async Task<CreateSaleResult> Handle(
        CreateSaleCommand request, CancellationToken ct)
    {
        var shift = await _shifts.GetOpenAsync(ct)
            ?? throw new DomainException(
                "No open shift — declare starting cash to start selling.");

        var method = await _paymentMethods.GetByIdAsync(request.PaymentMethodId, ct)
            ?? throw new NotFoundException("PaymentMethod", request.PaymentMethodId);
        if (!method.IsActive)
            throw new DomainException(
                $"{method.Name} is turned off — turn it on in web admin Settings.");

        var saleItems = new List<SaleItem>();
        var soldItems = new List<(Guid ItemId, int Quantity)>();
        var demand = new Dictionary<Guid, int>();
        decimal subtotal = 0;
        decimal totalLineDiscounts = 0;

        foreach (var cartItem in request.Items)
        {
            var item = await _items.GetByIdAsync(cartItem.ItemId, ct)
                ?? throw new NotFoundException("Item", cartItem.ItemId);

            decimal costPrice;
            if (item.IsComposite)
            {
                var components = await _composites.GetByParentIdAsync(item.Id, ct);
                foreach (var component in components)
                {
                    var required = (int)Math.Ceiling(component.Quantity * cartItem.Quantity);
                    demand[component.ComponentItemId] =
                        demand.GetValueOrDefault(component.ComponentItemId) + required;
                }
                costPrice = components.Sum(c => c.Quantity * c.ComponentItem.CostPrice);
            }
            else
            {
                demand[item.Id] = demand.GetValueOrDefault(item.Id) + cartItem.Quantity;
                costPrice = item.CostPrice;
            }

            var lineTotal = (item.SellingPrice * cartItem.Quantity) - cartItem.Discount;

            saleItems.Add(new SaleItem
            {
                ItemId = item.Id,
                ItemName = item.Name,
                UnitPrice = item.SellingPrice,
                CostPrice = costPrice,
                Quantity = cartItem.Quantity,
                Discount = cartItem.Discount,
                Total = lineTotal
            });

            soldItems.Add((item.Id, cartItem.Quantity));
            subtotal += item.SellingPrice * cartItem.Quantity;
            totalLineDiscounts += cartItem.Discount;
        }

        foreach (var (itemId, required) in demand)
        {
            var stockItem = await _items.GetByIdAsync(itemId, ct);
            if (stockItem is null || !stockItem.TracksStock) continue;
            if (stockItem.Stock < required)
                throw new InsufficientStockException(stockItem.Name, required, stockItem.Stock);
        }

        var totalDiscount = totalLineDiscounts + request.TransactionDiscount;
        var total = subtotal - totalDiscount;

        if (total < 0)
            throw new DomainException("Total cannot be negative after discounts.");

        if (request.AmountTendered < total)
            throw new DomainException(
                $"Amount tendered ({request.AmountTendered:N2}) is less than total ({total:N2}).");

        var receiptNumber = await _receiptGenerator.GenerateAsync(ct);
        var change = request.AmountTendered - total;

        var sale = new Sale
        {
            ReceiptNumber = receiptNumber,
            Subtotal = subtotal,
            DiscountAmount = totalDiscount,
            Total = total,
            PaymentMethodId = method.Id,
            AmountTendered = request.AmountTendered,
            ReferenceNumber = string.IsNullOrWhiteSpace(request.ReferenceNumber)
                ? null
                : request.ReferenceNumber.Trim(),
            Change = change,
            CreatedBy = _currentUser.Id,
            ShiftId = shift.Id,
            Items = saleItems
        };

        sale.AddDomainEvent(
            new SaleCompletedEvent(sale.Id, soldItems, _currentUser.Id));

        await _sales.AddAsync(sale, ct);
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await _unitOfWork.SaveChangesAsync(ct);
                break;
            }
            catch (ReceiptNumberCollisionException) when (attempt < 2)
            {
                sale.ReceiptNumber = await _receiptGenerator.GenerateAsync(ct);
            }
        }

        return new CreateSaleResult(
            sale.Id,
            sale.ReceiptNumber,
            subtotal,
            totalDiscount,
            total,
            sale.AmountTendered,
            change
        );
    }
}
