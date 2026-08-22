using MediatR;
using POS.Application.Common;
using POS.Application.Common.Interfaces;
using POS.Domain.Entities;
using POS.Domain.Enums;
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
    private readonly IStoreSettingsRepository _settings;
    private readonly IUtangRepository _utang;

    public CreateTransactionCommandHandler(
        IItemRepository itemRepository,
        ITransactionRepository transactionRepository,
        IReceiptNumberGenerator receiptGenerator,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ICompositeItemRepository compositeItemRepository,
        IShiftRepository shifts,
        IStoreSettingsRepository settings,
        IUtangRepository utang)
    {
        _itemRepository = itemRepository;
        _transactionRepository = transactionRepository;
        _receiptGenerator = receiptGenerator;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _compositeItemRepository = compositeItemRepository;
        _shifts = shifts;
        _settings = settings;
        _utang = utang;
    }

    public async Task<CreateTransactionResult> Handle(
        CreateTransactionCommand request, CancellationToken ct)
    {
        var shift = await _shifts.GetOpenAsync(ct)
            ?? throw new DomainException(
                "No open shift — declare starting cash to start selling.");

        var isUtang = request.PaymentType == PaymentType.Utang;
        Suki? suki = null;
        var defaultMarkup = 0m;
        if (isUtang)
        {
            var settings = await _settings.GetAsync(ct);
            if (settings?.AcceptUtang != true)
                throw new DomainException("Utang is off — turn it on in web admin Settings.");
            suki = await _utang.GetSukiByIdAsync(request.SukiId!.Value, ct)
                ?? throw new NotFoundException("Suki", request.SukiId.Value);
            defaultMarkup = settings.DefaultUtangMarkup;
        }
        decimal markupTotal = 0;

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

            var unitPrice = item.SellingPrice;
            if (isUtang)
            {
                var utangPrice = UtangPricing.Resolve(item, defaultMarkup, cartItem.Quantity);
                unitPrice = utangPrice.UnitPrice;
                markupTotal += utangPrice.MarkupPerUnit * cartItem.Quantity;
            }

            var lineTotal = (unitPrice * cartItem.Quantity) - cartItem.Discount;

            transactionItems.Add(new TransactionItem
            {
                ItemId = item.Id,
                ItemName = item.Name,
                UnitPrice = unitPrice,
                CostPrice = costPrice,
                Quantity = cartItem.Quantity,
                Discount = cartItem.Discount,
                Total = lineTotal
            });

            soldItems.Add((item.Id, cartItem.Quantity));
            subtotal += unitPrice * cartItem.Quantity;
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

        if (isUtang && request.DownPayment >= total)
            throw new DomainException(
                "The down payment covers the whole charge — ring it as a paid sale instead.");

        if (!isUtang && request.AmountTendered < total)
            throw new DomainException(
                $"Amount tendered ({request.AmountTendered:N2}) is less than total ({total:N2}).");

        var receiptNumber = await _receiptGenerator.GenerateAsync(ct);
        var change = isUtang ? 0m : request.AmountTendered - total;

        var transaction = new Transaction
        {
            ReceiptNumber = receiptNumber,
            Subtotal = subtotal,
            DiscountAmount = totalDiscount,
            Total = total,
            PaymentType = request.PaymentType,
            AmountTendered = isUtang ? 0m : request.AmountTendered,
            ReferenceNumber = string.IsNullOrWhiteSpace(request.ReferenceNumber)
                ? null
                : request.ReferenceNumber.Trim(),
            Change = change,
            CreatedBy = _currentUser.Id,
            ShiftId = shift.Id,
            SukiId = suki?.Id,
            Items = transactionItems
        };

        transaction.AddDomainEvent(
            new SaleCompletedEvent(transaction.Id, soldItems, _currentUser.Id));

        await _transactionRepository.AddAsync(transaction, ct);
        if (isUtang)
        {
            await _utang.AddEntryAsync(new UtangEntry
            {
                SukiId = suki!.Id,
                Type = UtangEntryType.Charge,
                Amount = total,
                Markup = markupTotal,
                Transaction = transaction,
                ShiftId = shift.Id,
                CreatedBy = _currentUser.Id
            }, ct);
            if (request.DownPayment > 0m)
            {
                await _utang.AddEntryAsync(new UtangEntry
                {
                    SukiId = suki.Id,
                    Type = UtangEntryType.Payment,
                    Amount = request.DownPayment,
                    Transaction = transaction,
                    Note = "Down payment",
                    ShiftId = shift.Id,
                    CreatedBy = _currentUser.Id
                }, ct);
            }
        }
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await _unitOfWork.SaveChangesAsync(ct);
                break;
            }
            catch (ReceiptNumberCollisionException) when (attempt < 2)
            {
                transaction.ReceiptNumber = await _receiptGenerator.GenerateAsync(ct);
            }
        }

        return new CreateTransactionResult(
            transaction.Id,
            transaction.ReceiptNumber,
            subtotal,
            totalDiscount,
            total,
            transaction.AmountTendered,
            change
        );
    }
}