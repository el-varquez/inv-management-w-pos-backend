using MediatR;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Items.Commands.DeleteItem;

public class DeleteItemCommandHandler : IRequestHandler<DeleteItemCommand>
{
    private readonly IItemRepository _itemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteItemCommandHandler(IItemRepository itemRepository, IUnitOfWork unitOfWork)
    {
        _itemRepository = itemRepository;
        _unitOfWork = unitOfWork;
    }    

    public async Task Handle(DeleteItemCommand request, CancellationToken ct)
    {
        var item = await _itemRepository.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Item", request.Id);

        await _itemRepository.DeleteAsync(item.Id, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}