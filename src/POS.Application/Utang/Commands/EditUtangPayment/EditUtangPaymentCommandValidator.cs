using FluentValidation;

namespace POS.Application.Utang.Commands.EditUtangPayment;

public class EditUtangPaymentCommandValidator
    : AbstractValidator<EditUtangPaymentCommand>
{
    public EditUtangPaymentCommandValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0m).WithMessage("Enter the corrected amount.");
    }
}
