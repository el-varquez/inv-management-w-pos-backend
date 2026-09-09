using MediatR;

namespace POS.Application.Sales.Commands.ProcessRefund;

public record ProcessRefundCommand(
    Guid SaleId
) : IRequest<RefundResult>;

public record RefundResult(
    Guid RefundSaleId,
    string ReceiptNumber,
    decimal RefundedAmount
);