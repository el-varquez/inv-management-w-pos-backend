using MediatR;

namespace POS.Application.Utang.Commands.CollectUtangPayment;

public record CollectUtangPaymentCommand(Guid SukiId, decimal Amount) : IRequest<Guid>;
