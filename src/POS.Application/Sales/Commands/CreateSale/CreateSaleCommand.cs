using MediatR;

namespace POS.Application.Sales.Commands.CreateSale;

public record CartItemInput(
    Guid ItemId,
    int Quantity,
    decimal Discount
);

public record CreateSaleCommand(
    IList<CartItemInput> Items,
    decimal TransactionDiscount,
    Guid PaymentMethodId,
    decimal AmountTendered,
    string? ReferenceNumber = null
) : IRequest<CreateSaleResult>;

public record CreateSaleResult(
    Guid SaleId,
    string ReceiptNumber,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal Total,
    decimal AmountTendered,
    decimal Change
);
