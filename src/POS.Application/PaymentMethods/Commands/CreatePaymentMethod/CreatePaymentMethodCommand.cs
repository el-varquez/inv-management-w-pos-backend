using MediatR;
using POS.Application.PaymentMethods.Queries.GetPaymentMethods;

namespace POS.Application.PaymentMethods.Commands.CreatePaymentMethod;

public record CreatePaymentMethodCommand(
    string Name, bool RequiresReference) : IRequest<PaymentMethodDto>;
