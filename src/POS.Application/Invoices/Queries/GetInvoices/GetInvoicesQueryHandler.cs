using MediatR;
using POS.Application.Common.Models;
using POS.Domain.Interfaces;

namespace POS.Application.Invoices.Queries.GetInvoices;

public class GetInvoicesQueryHandler
    : IRequestHandler<GetInvoicesQuery, PagedResult<InvoiceDto>>
{
    private readonly IInvoiceRepository _invoices;

    public GetInvoicesQueryHandler(IInvoiceRepository invoices) => _invoices = invoices;

    public async Task<PagedResult<InvoiceDto>> Handle(
        GetInvoicesQuery request, CancellationToken ct)
    {
        var (page, pageSize) = Pagination.Normalize(request.Page, request.PageSize);
        var (invoices, total) = await _invoices.GetPagedAsync(
            request.From, request.To, page, pageSize, ct);

        var dtos = invoices.Select(i => new InvoiceDto(
            i.Id, i.InvoiceNumber, i.SukiId, i.Suki.Name,
            i.Total, i.IsVoided, i.CreatedAt, i.CreatedBy)).ToList();

        return new PagedResult<InvoiceDto>(dtos, page, pageSize, total);
    }
}
