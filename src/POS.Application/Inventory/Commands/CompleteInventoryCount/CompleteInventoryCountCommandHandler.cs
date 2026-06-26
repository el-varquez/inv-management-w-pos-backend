using MediatR;
using POS.Application.Common.Interfaces;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Inventory.Commands.CompleteInventoryCount;

public class CompleteInventoryCountCommandHandler
    : IRequestHandler<CompleteInventoryCountCommand>
{
    private readonly IInventoryCountRepository _countRepository;
    private readonly IItemRepository _itemRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CompleteInventoryCountCommandHandler(
        IInventoryCountRepository countRepository,
        IItemRepository itemRepository,
        IStockMovementRepository stockMovementRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _countRepository = countRepository;
        _itemRepository = itemRepository;
        _stockMovementRepository = stockMovementRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(CompleteInventoryCountCommand request, CancellationToken ct)
    {
        var count = await _countRepository.GetByIdAsync(request.CountId, ct)
            ?? throw new NotFoundException("InventoryCount", request.CountId);

        if (count.Status == InventoryCountStatus.Completed)
            throw new DomainException("Inventory count is already completed.");

        var movements = new List<StockMovement>();

        foreach (var input in request.Lines)
        {
            var line = count.Lines.FirstOrDefault(l => l.ItemId == input.ItemId)
                ?? throw new DomainException($"Item line not found in this count.");

            var item = await _itemRepository.GetByIdAsync(input.ItemId, ct)
                ?? throw new NotFoundException("Item", input.ItemId);

            line.ActualQty = input.ActualQty;
            var variance = input.ActualQty - line.ExpectedQty;

            if (variance != 0)
            {
                item.Stock = input.ActualQty;
                item.UpdatedAt = DateTime.UtcNow;
                await _itemRepository.UpdateAsync(item, ct);

                movements.Add(new StockMovement
                {
                    ItemId = item.Id,
                    Type = StockMovementType.InventoryCount,
                    Quantity = variance,
                    Reason = variance < 0 ? AdjustmentReason.Loss : AdjustmentReason.Correction,
                    Notes = $"Inventory count {count.Reference}. Variance: {variance:+#;-#;0}",
                    CreatedBy = _currentUser.Id
                });
            }
        }

        if (movements.Any())
            await _stockMovementRepository.AddRangeAsync(movements, ct);

        count.Status = InventoryCountStatus.Completed;
        count.CompletedAt = DateTime.UtcNow;
        await _countRepository.UpdateAsync(count, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}