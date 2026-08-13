using FluentValidation;

namespace POS.Application.Items.Queries.SearchItems;

public class SearchItemsQueryValidator : AbstractValidator<SearchItemsQuery>
{
    public SearchItemsQueryValidator()
    {
        RuleFor(x => x.Term)
            .NotEmpty().WithMessage("Search term is required.");
    }
}
