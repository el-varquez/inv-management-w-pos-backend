using MediatR;

namespace POS.Application.Invoices.Commands.VoidInvoice;

public record VoidInvoiceCommand(Guid InvoiceId) : IRequest;
