using MediatR;
using POS.Application.Common.Interfaces;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Inventory.Commands.AdjustStock;

public class AdjustStockCommandHandler : IRequestHandler<AdjustStockCommand, Guid>
{
    private readonly IItemRepository _itemRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public AdjustStockCommandHandler(
        IItemRepository itemRepository,
        IStockMovementRepository stockMovementRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _itemRepository = itemRepository;
        _stockMovementRepository = stockMovementRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(AdjustStockCommand request, CancellationToken ct)
    {
        var item = await _itemRepository.GetByIdAsync(request.ItemId, ct)
            ?? throw new NotFoundException("Item", request.ItemId);

        var newStock = item.Stock + request.Quantity;
        if (newStock < 0)
            throw new DomainException(
                $"Adjustment would result in negative stock for '{item.Name}'. " +
                $"Current: {item.Stock}, Adjustment: {request.Quantity}.");

        item.Stock = newStock;
        item.UpdatedAt = DateTime.UtcNow;
        await _itemRepository.UpdateAsync(item, ct);

        var movement = new StockMovement
        {
            ItemId = item.Id,
            Type = StockMovementType.Adjustment,
            Quantity = request.Quantity,
            Reason = request.Reason,
            Notes = request.Notes,
            CreatedBy = _currentUser.Id
        };

        await _stockMovementRepository.AddAsync(movement, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return movement.Id;
    }
}