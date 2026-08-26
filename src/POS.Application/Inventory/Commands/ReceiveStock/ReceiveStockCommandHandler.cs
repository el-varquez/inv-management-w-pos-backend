using MediatR;
using POS.Application.Common;
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
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public ReceiveStockCommandHandler(
        IItemRepository itemRepository,
        IStockMovementRepository stockMovementRepository,
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _itemRepository = itemRepository;
        _stockMovementRepository = stockMovementRepository;
        _categoryRepository = categoryRepository;
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
        var resolved = new List<(Item Item, ReceiveStockLine Line, bool Created)>();
        List<string>? itemCodes = null;
        Guid? inventoryCategoryId = null;

        foreach (var line in request.Lines)
        {
            if (line.ItemId is { } itemId)
            {
                var item = await _itemRepository.GetByIdAsync(itemId, ct)
                    ?? throw new NotFoundException("Item", itemId);
                GuardReceivable(item);
                resolved.Add((item, line, false));
                continue;
            }

            var name = line.NewItem!.Name.Trim();
            var barcode = string.IsNullOrWhiteSpace(line.NewItem.Barcode)
                ? null
                : line.NewItem.Barcode.Trim();

            var existing = barcode is null
                ? null
                : await _itemRepository.GetByBarcodeAsync(barcode, ct);
            existing ??= await _itemRepository.GetByNameAsync(name, ct);

            if (existing is not null)
            {
                GuardReceivable(existing);
                resolved.Add((existing, line, false));
                continue;
            }

            inventoryCategoryId ??=
                (await _categoryRepository.GetByNameAsync(CategoryNames.InventoryItem, ct)
                    ?? throw new DomainException(
                        "The Inventory Item system category is missing.")).Id;
            itemCodes ??= (await _itemRepository.GetItemCodesAsync(ct)).ToList();
            var code = ItemCodeGenerator.Next(itemCodes);
            itemCodes.Add(code);

            var created = new Item
            {
                Name = name,
                Barcode = barcode,
                ItemCode = code,
                CostPrice = line.CostPerUnit,
                SellingPrice = line.SellingPrice,
                UtangMarkup = null,
                LowStockThreshold = 5,
                TracksStock = true,
                CategoryId = inventoryCategoryId.Value,
                Stock = 0
            };
            await _itemRepository.AddAsync(created, ct);
            resolved.Add((created, line, true));
        }

        if (resolved.Select(r => r.Item.Id).Distinct().Count() != resolved.Count)
            throw new DomainException("Each item can appear only once per delivery.");

        foreach (var (item, line, created) in resolved)
        {
            item.Stock += line.Quantity;
            item.CostPrice = line.CostPerUnit;
            item.SellingPrice = line.SellingPrice;
            item.UpdatedAt = DateTime.UtcNow;
            if (!created)
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

    private static void GuardReceivable(Item item)
    {
        if (item.IsComposite)
            throw new DomainException(
                $"\"{item.Name}\" is a composite item — its stock is built from components.");

        if (!item.TracksStock)
            throw new DomainException(
                $"\"{item.Name}\" is not a physical item — it has no stock to receive.");
    }
}
