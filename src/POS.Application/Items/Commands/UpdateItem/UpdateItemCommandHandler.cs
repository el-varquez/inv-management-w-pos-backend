using MediatR;
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

        item.Name = request.Name;
        item.Description = request.Description;
        item.Sku = request.Sku;
        item.CostPrice = request.CostPrice;
        item.SellingPrice = request.SellingPrice;
        item.LowStockThreshold = request.LowStockThreshold;
        item.CategoryId = request.CategoryId;
        item.IsActive = request.IsActive;
        item.UpdatedAt = DateTime.UtcNow;

        await _itemRepository.UpdateAsync(item, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}