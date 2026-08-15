using FluentValidation;

namespace POS.Application.Shifts.Commands.OpenShift;

public class OpenShiftCommandValidator : AbstractValidator<OpenShiftCommand>
{
    public OpenShiftCommandValidator()
    {
        RuleFor(x => x.StartingCash)
            .GreaterThan(0m).WithMessage("Starting cash must be greater than ₱0.");

        RuleFor(x => x.StartingEWalletBalance)
            .GreaterThanOrEqualTo(0m)
            .When(x => x.StartingEWalletBalance.HasValue)
            .WithMessage("Starting e-wallet balance cannot be negative.");
    }
}
