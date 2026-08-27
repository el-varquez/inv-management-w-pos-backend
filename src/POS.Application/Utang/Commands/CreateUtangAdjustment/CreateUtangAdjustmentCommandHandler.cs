using MediatR;
using POS.Application.Common.Interfaces;
using POS.Domain.Entities;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Utang.Commands.CreateUtangAdjustment;

public class CreateUtangAdjustmentCommandHandler
    : IRequestHandler<CreateUtangAdjustmentCommand, Guid>
{
    private readonly IUtangRepository _utang;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateUtangAdjustmentCommandHandler(
        IUtangRepository utang, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _utang = utang;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(
        CreateUtangAdjustmentCommand request, CancellationToken ct)
    {
        var suki = await _utang.GetSukiByIdAsync(request.SukiId, ct)
            ?? throw new NotFoundException("Suki", request.SukiId);

        var adjustment = new UtangAdjustment
        {
            SukiId = suki.Id,
            Amount = request.Amount,
            Note = request.Note.Trim(),
            CreatedBy = _currentUser.Id
        };

        await _utang.AddAdjustmentAsync(adjustment, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return adjustment.Id;
    }
}
