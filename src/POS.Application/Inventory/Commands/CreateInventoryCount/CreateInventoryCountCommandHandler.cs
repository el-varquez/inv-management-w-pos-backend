using MediatR;
using POS.Application.Common.Interfaces;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Domain.Interfaces;

namespace POS.Application.Inventory.Commands.CreateInventoryCount;

public class CreateInventoryCountCommandHandler
    : IRequestHandler<CreateInventoryCountCommand, Guid>
{
    private readonly IInventoryCountRepository _countRepository;
    private readonly IItemRepository _itemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateInventoryCountCommandHandler(
        IInventoryCountRepository countRepository,
        IItemRepository itemRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _countRepository = countRepository;
        _itemRepository = itemRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateInventoryCountCommand request, CancellationToken ct)
    {
        var items = await _itemRepository.GetAllAsync(ct);

        var lines = items.Select(item => new InventoryCountLine
        {
            ItemId = item.Id,
            ExpectedQty = item.Stock,
            ActualQty = item.Stock 
        }).ToList();

        var reference = $"COUNT-{DateTime.UtcNow:yyyyMMdd-HHmm}";

        var count = new InventoryCount
        {
            Reference = reference,
            Notes = request.Notes,
            Status = InventoryCountStatus.Draft,
            CreatedBy = _currentUser.Id,
            Lines = lines
        };

        await _countRepository.AddAsync(count, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return count.Id;
    }
}