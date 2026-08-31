using MediatR;

namespace POS.Application.PaymentMethods.Commands.UpdatePaymentMethod;

public record UpdatePaymentMethodCommand(
    Guid Id, string Name, bool RequiresReference, bool IsActive) : IRequest;
