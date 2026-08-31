using FluentValidation;

namespace POS.Application.Shifts.Commands.CloseShift;

public class CloseShiftCommandValidator : AbstractValidator<CloseShiftCommand>
{
    public CloseShiftCommandValidator()
    {
        RuleFor(x => x.CountedCash)
            .GreaterThanOrEqualTo(0m).WithMessage("Counted cash cannot be negative.");
    }
}
