using MediatR;
using POS.Application.Common.Interfaces;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Inventory.Commands.AddStock;

public class AddStockCommandHandler : IRequestHandler<AddStockCommand, Guid>
{
    private readonly IItemRepository _itemRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public AddStockCommandHandler(
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

    public async Task<Guid> Handle(AddStockCommand request, CancellationToken ct)
    {
        var item = await _itemRepository.GetByIdAsync(request.ItemId, ct)
            ?? throw new NotFoundException("Item", request.ItemId);

        item.Stock += request.Quantity;
        item.UpdatedAt = DateTime.UtcNow;
        await _itemRepository.UpdateAsync(item, ct);

        var movement = new StockMovement
        {
            ItemId = item.Id,
            Type = StockMovementType.AddStock,
            Quantity = request.Quantity,       
            CostPerUnit = request.CostPerUnit,
            SupplierName = request.SupplierName,
            Notes = request.Notes,
            CreatedBy = _currentUser.Id
        };

        await _stockMovementRepository.AddAsync(movement, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return movement.Id;
    }
}