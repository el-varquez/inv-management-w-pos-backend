using MediatR;

namespace POS.Application.Utang.Commands.VoidUtangPayment;

public record VoidUtangPaymentCommand(Guid Id) : IRequest;
