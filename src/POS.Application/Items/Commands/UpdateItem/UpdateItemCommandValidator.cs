using FluentValidation;

namespace POS.Application.Items.Commands.UpdateItem;

public class UpdateItemCommandValidator : AbstractValidator<UpdateItemCommand>
{
    public UpdateItemCommandValidator()
    {
        RuleFor(x => x.Barcode)
            .MaximumLength(64).WithMessage("Barcode must be 64 characters or fewer.");
    }
}
