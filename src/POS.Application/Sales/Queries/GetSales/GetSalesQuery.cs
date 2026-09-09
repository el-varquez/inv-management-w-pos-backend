using MediatR;
using POS.Application.Common.Models;

namespace POS.Application.Sales.Queries.GetSales;

public record GetSalesQuery(
    DateTime? From,
    DateTime? To,
    int? Page,
    int? PageSize
) : IRequest<PagedResult<SaleDto>>;

public record SaleDto(
    Guid Id,
    string ReceiptNumber,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal Total,
    Guid PaymentMethodId,
    string PaymentMethod,
    decimal AmountTendered,
    decimal Change,
    bool IsRefunded,
    Guid? RefundedFromId,
    int ItemCount,
    DateTime CreatedAt
);