using FluentValidation;

namespace POS.Application.Shifts.Commands.UpdateStartingCash;

public class UpdateStartingCashCommandValidator
    : AbstractValidator<UpdateStartingCashCommand>
{
    public UpdateStartingCashCommandValidator()
    {
        RuleFor(x => x.StartingCash)
            .GreaterThan(0m).WithMessage("Starting cash must be greater than ₱0.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("A reason is required.")
            .MaximumLength(256);
    }
}
