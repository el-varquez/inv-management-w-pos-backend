using MediatR;
using POS.Domain.Interfaces;

namespace POS.Application.PaymentMethods.Queries.GetPaymentMethods;

public class GetPaymentMethodsQueryHandler : IRequestHandler<GetPaymentMethodsQuery, IList<PaymentMethodDto>>
{
    private readonly IPaymentMethodRepository _methods;
    public GetPaymentMethodsQueryHandler(IPaymentMethodRepository methods) => _methods = methods;

    public async Task<IList<PaymentMethodDto>> Handle(GetPaymentMethodsQuery request, CancellationToken ct)
    {
        var methods = await _methods.GetAllAsync(ct);
        return methods
            .Select(m => new PaymentMethodDto(
                m.Id, m.Name, m.RequiresReference, m.IsActive, m.IsSystem))
            .ToList();
    }
}
