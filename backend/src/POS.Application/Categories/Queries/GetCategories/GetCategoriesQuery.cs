using MediatR;

namespace POS.Application.Categories.Queries.GetCategories;

public record GetCategoriesQuery : IRequest<IList<CategoryDto>>;

public record CategoryDto(
    Guid Id,
    string Name,
    string? Description,
    int ItemCount
);