using FluentValidation;

namespace POS.Application.Shifts.Commands.CorrectShiftCount;

public class CorrectShiftCountCommandValidator
    : AbstractValidator<CorrectShiftCountCommand>
{
    public CorrectShiftCountCommandValidator()
    {
        RuleFor(x => x.CountedCash)
            .GreaterThanOrEqualTo(0m).WithMessage("Counted cash cannot be negative.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("A reason is required.")
            .MaximumLength(256);
    }
}
