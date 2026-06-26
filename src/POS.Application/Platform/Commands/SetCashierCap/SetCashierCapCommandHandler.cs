using MediatR;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Platform.Commands.SetCashierCap;

public class SetCashierCapCommandHandler : IRequestHandler<SetCashierCapCommand>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetCashierCapCommandHandler(
        ITenantRepository tenantRepository,
        IUnitOfWork unitOfWork)
    {
        _tenantRepository = tenantRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(SetCashierCapCommand request, CancellationToken ct)
    {
        var tenant = await _tenantRepository.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Tenant", request.Id);

        tenant.CashierCap = request.CashierCap;
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
