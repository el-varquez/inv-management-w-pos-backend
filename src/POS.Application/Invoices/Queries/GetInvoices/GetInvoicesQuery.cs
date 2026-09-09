using MediatR;
using POS.Application.Common.Models;

namespace POS.Application.Invoices.Queries.GetInvoices;

public record GetInvoicesQuery(
    DateTime? From,
    DateTime? To,
    int? Page,
    int? PageSize
) : IRequest<PagedResult<InvoiceDto>>;

public record InvoiceDto(
    Guid Id,
    string InvoiceNumber,
    Guid SukiId,
    string SukiName,
    decimal Total,
    bool IsVoided,
    DateTime CreatedAt,
    Guid CreatedBy
);
