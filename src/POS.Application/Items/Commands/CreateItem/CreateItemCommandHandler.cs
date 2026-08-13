using MediatR;
using POS.Domain.Entities;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Items.Commands.CreateItem;

public class CreateItemCommandHandler : IRequestHandler<CreateItemCommand, Guid>
{
    private readonly IItemRepository _itemRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateItemCommandHandler(
        IItemRepository itemRepository,
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork
    )
    {
        _itemRepository = itemRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateItemCommand request, CancellationToken ct)
    {
        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, ct)
            ?? throw new NotFoundException(nameof(Category), request.CategoryId);

        var barcode = string.IsNullOrWhiteSpace(request.Barcode)
            ? null
            : request.Barcode.Trim();
        if (barcode is not null)
        {
            var existing = await _itemRepository.GetByBarcodeAsync(barcode, ct);
            if (existing is not null)
                throw new DomainException(
                    $"Barcode {barcode} is already used by \"{existing.Name}\".");
        }

        var item = new Item
        {
            Name = request.Name,
            Description = request.Description,
            Sku = request.Sku,
            Barcode = barcode,
            CostPrice = request.CostPrice,
            SellingPrice = request.SellingPrice,
            LowStockThreshold = request.LowStockThreshold,
            CategoryId = request.CategoryId,
            Stock = 0
        };

        await _itemRepository.AddAsync(item, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return item.Id;
    }
}