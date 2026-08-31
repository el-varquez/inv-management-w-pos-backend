using MediatR;
using POS.Application.PaymentMethods.Queries.GetPaymentMethods;
using POS.Domain.Enums;

namespace POS.Application.PaymentMethods.Commands.CreatePaymentMethod;

public record CreatePaymentMethodCommand(
    string Name, PaymentMethodType Type, bool RequiresReference) : IRequest<PaymentMethodDto>;
