using MediatR;

namespace POS.Application.Utang.Commands.CollectUtangPayment;

public record CollectUtangPaymentCommand(
    Guid SukiId, decimal Amount, string? Note = null) : IRequest<Guid>;
