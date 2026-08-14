using MediatR;
using POS.Application.Common.Interfaces;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Inventory.Commands.ReceiveStock;

public class ReceiveStockCommandHandler : IRequestHandler<ReceiveStockCommand, int>
{
    private readonly IItemRepository _itemRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public ReceiveStockCommandHandler(
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

    public async Task<int> Handle(ReceiveStockCommand request, CancellationToken ct)
    {
        var supplier = string.IsNullOrWhiteSpace(request.SupplierName)
            ? null
            : request.SupplierName.Trim();
        var notes = string.IsNullOrWhiteSpace(request.Notes)
            ? null
            : request.Notes.Trim();

        // Resolve and validate every line before mutating anything,
        // so a bad line rejects the whole delivery.
        var resolved = new List<(Item Item, ReceiveStockLine Line)>();
        foreach (var line in request.Lines)
        {
            var item = await _itemRepository.GetByIdAsync(line.ItemId, ct)
                ?? throw new NotFoundException("Item", line.ItemId);

            if (item.IsComposite)
                throw new DomainException(
                    $"\"{item.Name}\" is a composite item — its stock is built from components.");

            if (!item.TracksStock)
                throw new DomainException(
                    $"\"{item.Name}\" is not a physical item — it has no stock to receive.");

            resolved.Add((item, line));
        }

        foreach (var (item, line) in resolved)
        {
            item.Stock += line.Quantity;
            item.CostPrice = line.CostPerUnit;
            item.SellingPrice = line.SellingPrice;
            item.UpdatedAt = DateTime.UtcNow;
            await _itemRepository.UpdateAsync(item, ct);

            await _stockMovementRepository.AddAsync(new StockMovement
            {
                ItemId = item.Id,
                Type = StockMovementType.AddStock,
                Quantity = line.Quantity,
                CostPerUnit = line.CostPerUnit,
                SupplierName = supplier,
                Notes = notes,
                CreatedBy = _currentUser.Id
            }, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        return request.Lines.Count;
    }
}
