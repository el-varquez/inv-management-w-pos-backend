using MediatR;
using POS.Domain.Enums;

namespace POS.Application.Sales.Commands.CreateTransaction;

public record CartItemInput(
    Guid ItemId,
    int Quantity,
    decimal Discount       
);

public record CreateTransactionCommand(
    IList<CartItemInput> Items,
    decimal TransactionDiscount,
    PaymentType PaymentType,
    decimal AmountTendered,
    string? ReferenceNumber = null,
    Guid? SukiId = null,
    decimal DownPayment = 0m
) : IRequest<CreateTransactionResult>;

public record CreateTransactionResult(
    Guid TransactionId,
    string ReceiptNumber,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal Total,
    decimal AmountTendered,
    decimal Change
);