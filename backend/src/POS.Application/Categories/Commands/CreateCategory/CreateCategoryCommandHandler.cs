using MediatR;
using POS.Domain.Entities;
using POS.Domain.Interfaces;

namespace POS.Application.Categories.Commands.CreateCategory;

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Guid>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCategoryCommandHandler(ICategoryRepository categoryRepository, IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }
    
    public async Task<Guid> Handle(CreateCategoryCommand request, CancellationToken ct)
    {
        var category = new Category
        {
            Name = request.Name,
            Description = request.Description
        };

        await _categoryRepository.AddAsync(category, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return category.Id;
    }
}