using MediatR;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Categories.Commands.DeleteCategory;

public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCategoryCommandHandler(
        ICategoryRepository categoryRepository, IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteCategoryCommand request, CancellationToken ct)
    {
        var category = await _categoryRepository.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Category", request.Id);

        // Items have a Restrict FK to their category, so block deletion while any
        // remain — surface a clear message instead of a raw DB constraint error.
        if (category.Items.Count > 0)
            throw new DomainException(
                $"Cannot delete '{category.Name}' — it still has {category.Items.Count} " +
                $"item{(category.Items.Count == 1 ? "" : "s")}. Move or remove them first.");

        await _categoryRepository.DeleteAsync(request.Id, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
