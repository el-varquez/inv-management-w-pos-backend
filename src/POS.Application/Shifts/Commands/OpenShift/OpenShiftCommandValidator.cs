using FluentValidation;

namespace POS.Application.Shifts.Commands.OpenShift;

public class OpenShiftCommandValidator : AbstractValidator<OpenShiftCommand>
{
    public OpenShiftCommandValidator()
    {
        RuleFor(x => x.StartingCash)
            .GreaterThan(0m).WithMessage("Starting cash must be greater than ₱0.");
    }
}
