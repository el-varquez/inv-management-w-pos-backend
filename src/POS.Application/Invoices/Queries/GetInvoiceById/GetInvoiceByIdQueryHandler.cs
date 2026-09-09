using MediatR;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Invoices.Queries.GetInvoiceById;

public class GetInvoiceByIdQueryHandler
    : IRequestHandler<GetInvoiceByIdQuery, InvoiceDetailDto>
{
    private readonly IInvoiceRepository _invoices;

    public GetInvoiceByIdQueryHandler(IInvoiceRepository invoices) => _invoices = invoices;

    public async Task<InvoiceDetailDto> Handle(
        GetInvoiceByIdQuery request, CancellationToken ct)
    {
        var i = await _invoices.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Invoice", request.Id);

        return new InvoiceDetailDto(
            i.Id, i.InvoiceNumber, i.SukiId, i.Suki.Name,
            i.Subtotal, i.DiscountAmount, i.MarkupTotal, i.Total,
            i.ShiftId, i.IsVoided, i.VoidedAt, i.VoidedBy,
            i.Items.Select(l => new InvoiceLineDto(
                l.ItemName, l.UnitPrice, l.Quantity, l.Discount, l.Total)).ToList(),
            i.CreatedAt, i.CreatedBy);
    }
}
