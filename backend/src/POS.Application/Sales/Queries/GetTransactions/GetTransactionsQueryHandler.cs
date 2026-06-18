using MediatR;
using POS.Application.Common.Models;
using POS.Domain.Interfaces;

namespace POS.Application.Sales.Queries.GetTransactions;

public class GetTransactionsQueryHandler
    : IRequestHandler<GetTransactionsQuery, PagedResult<TransactionDto>>
{
    private readonly ITransactionRepository _transactionRepository;

    public GetTransactionsQueryHandler(ITransactionRepository transactionRepository)
        => _transactionRepository = transactionRepository;

    public async Task<PagedResult<TransactionDto>> Handle(
        GetTransactionsQuery request, CancellationToken ct)
    {
        var (page, pageSize) = Pagination.Normalize(request.Page, request.PageSize);
        var (transactions, total) = await _transactionRepository.GetPagedAsync(
            request.From, request.To, page, pageSize, ct);

        var dtos = transactions.Select(t => new TransactionDto(
            t.Id,
            t.ReceiptNumber,
            t.Subtotal,
            t.DiscountAmount,
            t.Total,
            t.PaymentType.ToString(),
            t.AmountTendered,
            t.Change,
            t.IsRefunded,
            t.RefundedFromId,
            t.Items.Count,
            t.CreatedAt
        )).ToList();

        return new PagedResult<TransactionDto>(dtos, page, pageSize, total);
    }
}
