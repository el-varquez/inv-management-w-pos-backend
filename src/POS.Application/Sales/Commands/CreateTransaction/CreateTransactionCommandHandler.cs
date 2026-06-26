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

    public CreateTransactionCommandHandler(
        IItemRepository itemRepository,
        ITransactionRepository transactionRepository,
        IReceiptNumberGenerator receiptGenerator,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _itemRepository = itemRepository;
        _transactionRepository = transactionRepository;
        _receiptGenerator = receiptGenerator;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<CreateTransactionResult> Handle(
        CreateTransactionCommand request, CancellationToken ct)
    {
        var transactionItems = new List<TransactionItem>();
        var soldItems = new List<(Guid ItemId, int Quantity)>();
        decimal subtotal = 0;
        decimal totalLineDiscounts = 0;

        foreach (var cartItem in request.Items)
        {
            var item = await _itemRepository.GetByIdAsync(cartItem.ItemId, ct)
                ?? throw new NotFoundException("Item", cartItem.ItemId);

            if (!item.IsComposite && item.Stock < cartItem.Quantity)
                throw new InsufficientStockException(
                    item.Name, cartItem.Quantity, item.Stock);

            var lineTotal = (item.SellingPrice * cartItem.Quantity) - cartItem.Discount;

            transactionItems.Add(new TransactionItem
            {
                ItemId = item.Id,
                ItemName = item.Name,              
                UnitPrice = item.SellingPrice,     
                CostPrice = item.CostPrice,        
                Quantity = cartItem.Quantity,
                Discount = cartItem.Discount,
                Total = lineTotal
            });

            soldItems.Add((item.Id, cartItem.Quantity));
            subtotal += item.SellingPrice * cartItem.Quantity;
            totalLineDiscounts += cartItem.Discount;
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