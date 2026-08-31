using MediatR;
using POS.Domain.Entities;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.PaymentMethods.Commands.UpdatePaymentMethod;

public class UpdatePaymentMethodCommandHandler : IRequestHandler<UpdatePaymentMethodCommand>
{
    private readonly IPaymentMethodRepository _methods;
    private readonly IUnitOfWork _unitOfWork;

    public UpdatePaymentMethodCommandHandler(IPaymentMethodRepository methods, IUnitOfWork unitOfWork)
    {
        _methods = methods;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdatePaymentMethodCommand request, CancellationToken ct)
    {
        var method = await _methods.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("PaymentMethod", request.Id);

        var duplicate = await _methods.GetByNameAsync(request.Name, ct);
        if (duplicate is not null && duplicate.Id != method.Id)
            throw new DomainException($"A payment method named \"{request.Name}\" already exists.");

        if (method.Id == PaymentMethodIds.Cash && !request.IsActive)
            throw new DomainException("Cash can't be turned off.");

        method.Name = request.Name;
        method.RequiresReference = request.RequiresReference;
        method.IsActive = request.IsActive;
        method.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
