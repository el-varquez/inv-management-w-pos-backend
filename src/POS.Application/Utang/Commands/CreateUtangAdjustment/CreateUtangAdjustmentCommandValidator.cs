using FluentValidation;

namespace POS.Application.Utang.Commands.CreateUtangAdjustment;

public class CreateUtangAdjustmentCommandValidator
    : AbstractValidator<CreateUtangAdjustmentCommand>
{
    public CreateUtangAdjustmentCommandValidator()
    {
        RuleFor(x => x.Amount)
            .NotEqual(0m)
            .WithMessage("Enter an amount — use a minus sign to reduce the balance.");

        RuleFor(x => x.Note)
            .NotEmpty().WithMessage("Say what this adjustment is for.")
            .MaximumLength(200);
    }
}
