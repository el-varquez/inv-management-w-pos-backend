using MediatR;

namespace POS.Application.Sales.Commands.ProcessRefund;

public record ProcessRefundCommand(
    Guid TransactionId
) : IRequest<RefundResult>;

public record RefundResult(
    Guid RefundTransactionId,
    string ReceiptNumber,
    decimal RefundedAmount
);