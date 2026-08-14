using MediatR;
using POS.Application.Common;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Items.Commands.UpdateItem;

public class UpdateItemCommandHandler : IRequestHandler<UpdateItemCommand>
{
    private readonly IItemRepository _itemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateItemCommandHandler(IItemRepository itemRepository, IUnitOfWork unitOfWork)
    {
        _itemRepository = itemRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateItemCommand request, CancellationToken ct)
    {
        var item = await _itemRepository.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Item", request.Id);

        var barcode = string.IsNullOrWhiteSpace(request.Barcode)
            ? null
            : request.Barcode.Trim();
        if (barcode is not null)
        {
            var existing = await _itemRepository.GetByBarcodeAsync(barcode, ct);
            if (existing is not null && existing.Id != request.Id)
                throw new DomainException(
                    $"Barcode {barcode} is already used by \"{existing.Name}\".");
        }

        var itemCode = string.IsNullOrWhiteSpace(request.ItemCode)
            ? null
            : request.ItemCode.Trim();
        if (itemCode is not null)
        {
            var existing = await _itemRepository.GetByItemCodeAsync(itemCode, ct);
            if (existing is not null && existing.Id != request.Id)
                throw new DomainException(
                    $"Item code {itemCode} is already used by \"{existing.Name}\".");
        }
        else
        {
            itemCode = ItemCodeGenerator.Next(await _itemRepository.GetItemCodesAsync(ct));
        }

        item.Name = request.Name;
        item.Description = request.Description;
        item.ItemCode = itemCode;
        item.Barcode = barcode;
        item.CostPrice = request.CostPrice;
        item.SellingPrice = request.SellingPrice;
        item.UtangMarkup = request.UtangMarkup;
        item.LowStockThreshold = request.LowStockThreshold;
        item.TracksStock = request.TracksStock;
        item.CategoryId = request.CategoryId;
        item.IsActive = request.IsActive;
        item.UpdatedAt = DateTime.UtcNow;

        await _itemRepository.UpdateAsync(item, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}