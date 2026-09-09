using MediatR;

namespace POS.Application.Invoices.Commands.CreateInvoice;

public record InvoiceLineInput(Guid ItemId, int Quantity, decimal Discount);

public record CreateInvoiceCommand(
    IList<InvoiceLineInput> Items,
    decimal InvoiceDiscount,
    Guid SukiId
) : IRequest<CreateInvoiceResult>;

public record CreateInvoiceResult(
    Guid InvoiceId,
    string InvoiceNumber,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal MarkupTotal,
    decimal Total,
    decimal NewBalance
);
