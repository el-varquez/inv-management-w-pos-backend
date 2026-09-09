using MediatR;

namespace POS.Application.PaymentMethods.Queries.GetPaymentMethods;

public record GetPaymentMethodsQuery : IRequest<IList<PaymentMethodDto>>;

public record PaymentMethodDto(
    Guid Id, string Name, bool RequiresReference, bool IsActive, bool IsSystem);
