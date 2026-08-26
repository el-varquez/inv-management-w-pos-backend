using MediatR;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Categories.Commands.UpdateCategory;

public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCategoryCommandHandler(
        ICategoryRepository categoryRepository, IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateCategoryCommand request, CancellationToken ct)
    {
        var category = await _categoryRepository.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Category", request.Id);

        if (category.IsSystem)
            throw new DomainException($"\"{category.Name}\" is a system category — it can't be renamed.");

        var name = request.Name.Trim();
        var existing = await _categoryRepository.GetByNameAsync(name, ct);
        if (existing is not null && existing.Id != request.Id)
            throw new DomainException($"A category named \"{name}\" already exists.");

        category.Name = name;
        category.Description = request.Description;
        category.UpdatedAt = DateTime.UtcNow;

        await _categoryRepository.UpdateAsync(category, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
