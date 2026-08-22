using MediatR;

namespace POS.Application.Utang.Commands.EditUtangPayment;

public record EditUtangPaymentCommand(Guid Id, decimal Amount) : IRequest;
