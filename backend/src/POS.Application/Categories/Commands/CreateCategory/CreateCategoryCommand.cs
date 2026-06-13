using MediatR;

namespace POS.Application.Categories.Commands.CreateCategory;

public record CreateCategoryCommand(
    string Name,
    string? Description
) : IRequest<Guid>;