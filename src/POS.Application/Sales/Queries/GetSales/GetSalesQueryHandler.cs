using MediatR;
using POS.Application.Common.Models;
using POS.Domain.Interfaces;

namespace POS.Application.Sales.Queries.GetSales;

public class GetSalesQueryHandler
    : IRequestHandler<GetSalesQuery, PagedResult<SaleDto>>
{
    private readonly ISaleRepository _sales;

    public GetSalesQueryHandler(ISaleRepository saleRepository)
        => _sales = saleRepository;

    public async Task<PagedResult<SaleDto>> Handle(
        GetSalesQuery request, CancellationToken ct)
    {
        var (page, pageSize) = Pagination.Normalize(request.Page, request.PageSize);
        var (sales, total) = await _sales.GetPagedAsync(
            request.From, request.To, page, pageSize, ct);

        var dtos = sales.Select(t => new SaleDto(
            t.Id,
            t.ReceiptNumber,
            t.Subtotal,
            t.DiscountAmount,
            t.Total,
            t.PaymentMethodId,
            t.PaymentMethod!.Name,
            t.AmountTendered,
            t.Change,
            t.IsRefunded,
            t.RefundedFromId,
            t.Items.Count,
            t.CreatedAt
        )).ToList();

        return new PagedResult<SaleDto>(dtos, page, pageSize, total);
    }
}
