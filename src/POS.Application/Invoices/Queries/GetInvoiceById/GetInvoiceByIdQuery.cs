using MediatR;

namespace POS.Application.Invoices.Queries.GetInvoiceById;

public record GetInvoiceByIdQuery(Guid Id) : IRequest<InvoiceDetailDto>;

public record InvoiceLineDto(
    string ItemName,
    decimal UnitPrice,
    int Quantity,
    decimal Discount,
    decimal Total
);

public record InvoiceDetailDto(
    Guid Id,
    string InvoiceNumber,
    Guid SukiId,
    string SukiName,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal MarkupTotal,
    decimal Total,
    Guid ShiftId,
    bool IsVoided,
    DateTime? VoidedAt,
    Guid? VoidedBy,
    IList<InvoiceLineDto> Lines,
    DateTime CreatedAt,
    Guid CreatedBy
);
