using MediatR;

namespace POS.Application.Sales.Queries.GetSaleById;

public record GetSaleByIdQuery(Guid Id) : IRequest<SaleDetailDto>;

public record SaleLineDto(
    string ItemName,
    decimal UnitPrice,
    int Quantity,
    decimal Discount,
    decimal Total
);

public record SaleDetailDto(
    Guid Id,
    string ReceiptNumber,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal Total,
    Guid PaymentMethodId,
    string PaymentMethod,
    string? ReferenceNumber,
    decimal AmountTendered,
    decimal Change,
    bool IsRefunded,
    IList<SaleLineDto> Lines,
    DateTime CreatedAt
);