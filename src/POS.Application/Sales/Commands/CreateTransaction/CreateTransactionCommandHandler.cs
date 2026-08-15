using MediatR;
using POS.Application.Common.Interfaces;
using POS.Domain.Entities;
using POS.Domain.Events;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Sales.Commands.CreateTransaction;

public class CreateTransactionCommandHandler
    : IRequestHandler<CreateTransactionCommand, CreateTransactionResult>
{
    private readonly IItemRepository _itemRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IReceiptNumberGenerator _receiptGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ICompositeItemRepository _compositeItemRepository;
    private readonly IShiftRepository _shifts;

    public CreateTransactionCommandHandler(
        IItemRepository itemRepository,
        ITransactionRepository transactionRepository,
        IReceiptNumberGenerator receiptGenerator,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ICompositeItemRepository compositeItemRepository,
        IShiftRepository shifts)
    {
        _itemRepository = itemRepository;
        _transactionRepository = transactionRepository;
        _receiptGenerator = receiptGenerator;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _compositeItemRepository = compositeItemRepository;
        _shifts = shifts;
    }

    public async Task<CreateTransactionResult> Handle(
        CreateTransactionCommand request, CancellationToken ct)
    {
        var shift = await _shifts.GetOpenAsync(ct)
            ?? throw new DomainException(
                "No open shift — declare starting cash to start selling.");

        var transactionItems = new List<TransactionItem>();
        var soldItems = new List<(Guid ItemId, int Quantity)>();
        var demand = new Dictionary<Guid, int>();
        decimal subtotal = 0;
        decimal totalLineDiscounts = 0;

        foreach (var cartItem in request.Items)
        {
            var item = await _itemRepository.GetByIdAsync(cartItem.ItemId, ct)
                ?? throw new NotFoundException("Item", cartItem.ItemId);

            decimal costPrice;
            if (item.IsComposite)
            {
                var components = await _compositeItemRepository.GetByParentIdAsync(item.Id, ct);
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

            transactionItems.Add(new TransactionItem
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
            var stockItem = await _itemRepository.GetByIdAsync(itemId, ct);
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

        var transaction = new Transaction
        {
            ReceiptNumber = receiptNumber,
            Subtotal = subtotal,
            DiscountAmount = totalDiscount,
            Total = total,
            PaymentType = request.PaymentType,
            AmountTendered = request.AmountTendered,
            Change = change,
            CreatedBy = _currentUser.Id,
            ShiftId = shift.Id,
            Items = transactionItems
        };

        transaction.AddDomainEvent(
            new SaleCompletedEvent(transaction.Id, soldItems, _currentUser.Id));

        await _transactionRepository.AddAsync(transaction, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new CreateTransactionResult(
            transaction.Id,
            receiptNumber,
            subtotal,
            totalDiscount,
            total,
            request.AmountTendered,
            change
        );
    }
}