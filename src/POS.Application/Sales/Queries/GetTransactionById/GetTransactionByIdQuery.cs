using MediatR;

namespace POS.Application.Sales.Queries.GetTransactionById;

public record GetTransactionByIdQuery(Guid Id) : IRequest<TransactionDetailDto>;

public record TransactionLineDto(
    string ItemName,
    decimal UnitPrice,
    int Quantity,
    decimal Discount,
    decimal Total
);

public record TransactionDetailDto(
    Guid Id,
    string ReceiptNumber,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal Total,
    string PaymentType,
    decimal AmountTendered,
    decimal Change,
    bool IsRefunded,
    IList<TransactionLineDto> Lines,
    DateTime CreatedAt
);