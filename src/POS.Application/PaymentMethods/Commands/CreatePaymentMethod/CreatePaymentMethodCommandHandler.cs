using MediatR;
using POS.Application.PaymentMethods.Queries.GetPaymentMethods;
using POS.Domain.Entities;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.PaymentMethods.Commands.CreatePaymentMethod;

public class CreatePaymentMethodCommandHandler : IRequestHandler<CreatePaymentMethodCommand, PaymentMethodDto>
{
    private readonly IPaymentMethodRepository _methods;
    private readonly IUnitOfWork _unitOfWork;

    public CreatePaymentMethodCommandHandler(IPaymentMethodRepository methods, IUnitOfWork unitOfWork)
    {
        _methods = methods;
        _unitOfWork = unitOfWork;
    }

    public async Task<PaymentMethodDto> Handle(CreatePaymentMethodCommand request, CancellationToken ct)
    {
        var existing = await _methods.GetByNameAsync(request.Name, ct);
        if (existing is not null)
            throw new DomainException($"A payment method named \"{request.Name}\" already exists.");

        var method = new PaymentMethod
        {
            Name = request.Name,
            Type = request.Type,
            RequiresReference = request.RequiresReference,
            IsActive = true,
            IsSystem = false
        };

        await _methods.AddAsync(method, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new PaymentMethodDto(
            method.Id, method.Name, method.Type.ToString(), method.RequiresReference, method.IsActive, method.IsSystem);
    }
}
