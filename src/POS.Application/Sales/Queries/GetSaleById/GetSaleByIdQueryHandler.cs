using MediatR;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Sales.Queries.GetSaleById;

public class GetSaleByIdQueryHandler
    : IRequestHandler<GetSaleByIdQuery, SaleDetailDto>
{
    private readonly ISaleRepository _sales;

    public GetSaleByIdQueryHandler(ISaleRepository saleRepository)
        => _sales = saleRepository;

    public async Task<SaleDetailDto> Handle(
        GetSaleByIdQuery request, CancellationToken ct)
    {
        var t = await _sales.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Sale", request.Id);

        return new SaleDetailDto(
            t.Id,
            t.ReceiptNumber,
            t.Subtotal,
            t.DiscountAmount,
            t.Total,
            t.PaymentMethodId,
            t.PaymentMethod!.Name,
            t.ReferenceNumber,
            t.AmountTendered,
            t.Change,
            t.IsRefunded,
            t.Items.Select(i => new SaleLineDto(
                i.ItemName,
                i.UnitPrice,
                i.Quantity,
                i.Discount,
                i.Total
            )).ToList(),
            t.CreatedAt
        );
    }
}