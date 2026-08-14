using FluentValidation;

namespace POS.Application.Items.Commands.UpdateItem;

public class UpdateItemCommandValidator : AbstractValidator<UpdateItemCommand>
{
    public UpdateItemCommandValidator()
    {
        RuleFor(x => x.Barcode)
            .MaximumLength(64).WithMessage("Barcode must be 64 characters or fewer.");

        RuleFor(x => x.UtangMarkup)
            .GreaterThanOrEqualTo(0m).WithMessage("Utang markup must be 0 or greater.")
            .Must(m => decimal.Round(m!.Value, 2) == m.Value)
                .WithMessage("Utang markup cannot have more than 2 decimal places.")
            .When(x => x.UtangMarkup.HasValue);
    }
}
