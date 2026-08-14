using FluentValidation;

namespace POS.Application.Items.Commands.CreateItem;

public class CreateItemCommandValidator : AbstractValidator<CreateItemCommand>
{
    public CreateItemCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Item name is required.")
            .MaximumLength(100);

        RuleFor(x => x.Barcode)
            .MaximumLength(64).WithMessage("Barcode must be 64 characters or fewer.");

        RuleFor(x => x.CostPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Cost price must be 0 or greater.");

        RuleFor(x => x.SellingPrice)
            .GreaterThan(0).WithMessage("Selling price must be greater than 0.");
            
        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category is required.");

        RuleFor(x => x.UtangMarkup)
            .GreaterThanOrEqualTo(0m).WithMessage("Utang markup must be 0 or greater.")
            .Must(m => decimal.Round(m!.Value, 2) == m.Value)
                .WithMessage("Utang markup cannot have more than 2 decimal places.")
            .When(x => x.UtangMarkup.HasValue);
    }
}