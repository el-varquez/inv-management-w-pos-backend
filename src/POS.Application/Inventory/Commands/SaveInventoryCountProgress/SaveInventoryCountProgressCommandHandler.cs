using MediatR;
using POS.Domain.Enums;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Inventory.Commands.SaveInventoryCountProgress;

public class SaveInventoryCountProgressCommandHandler
    : IRequestHandler<SaveInventoryCountProgressCommand>
{
    private readonly IInventoryCountRepository _countRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SaveInventoryCountProgressCommandHandler(
        IInventoryCountRepository countRepository, IUnitOfWork unitOfWork)
    {
        _countRepository = countRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(SaveInventoryCountProgressCommand request, CancellationToken ct)
    {
        var count = await _countRepository.GetByIdAsync(request.CountId, ct)
            ?? throw new NotFoundException("InventoryCount", request.CountId);

        if (count.Status == InventoryCountStatus.Completed)
            throw new DomainException("Cannot edit a completed inventory count.");

        foreach (var input in request.Lines)
        {
            var line = count.Lines.FirstOrDefault(l => l.ItemId == input.ItemId)
                ?? throw new DomainException("Item line not found in this count.");
            line.ActualQty = input.ActualQty;
        }

        await _countRepository.UpdateAsync(count, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
