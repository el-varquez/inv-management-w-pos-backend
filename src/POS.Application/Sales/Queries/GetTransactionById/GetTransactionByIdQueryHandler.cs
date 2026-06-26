using MediatR;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Sales.Queries.GetTransactionById;

public class GetTransactionByIdQueryHandler
    : IRequestHandler<GetTransactionByIdQuery, TransactionDetailDto>
{
    private readonly ITransactionRepository _transactionRepository;

    public GetTransactionByIdQueryHandler(ITransactionRepository transactionRepository)
        => _transactionRepository = transactionRepository;

    public async Task<TransactionDetailDto> Handle(
        GetTransactionByIdQuery request, CancellationToken ct)
    {
        var t = await _transactionRepository.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Transaction", request.Id);

        return new TransactionDetailDto(
            t.Id,
            t.ReceiptNumber,
            t.Subtotal,
            t.DiscountAmount,
            t.Total,
            t.PaymentType.ToString(),
            t.AmountTendered,
            t.Change,
            t.IsRefunded,
            t.Items.Select(i => new TransactionLineDto(
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