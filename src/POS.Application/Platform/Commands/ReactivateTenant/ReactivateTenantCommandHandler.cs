using MediatR;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Platform.Commands.ReactivateTenant;

public class ReactivateTenantCommandHandler : IRequestHandler<ReactivateTenantCommand>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ReactivateTenantCommandHandler(
        ITenantRepository tenantRepository,
        IUnitOfWork unitOfWork)
    {
        _tenantRepository = tenantRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ReactivateTenantCommand request, CancellationToken ct)
    {
        var tenant = await _tenantRepository.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Tenant", request.Id);

        tenant.IsActive = true;
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
