using FluentValidation;

namespace POS.Application.Shifts.Commands.CloseShift;

public class CloseShiftCommandValidator : AbstractValidator<CloseShiftCommand>
{
    public CloseShiftCommandValidator()
    {
        RuleFor(x => x.CountedCash)
            .GreaterThanOrEqualTo(0m).WithMessage("Counted cash cannot be negative.");

        RuleFor(x => x.CountedEWalletBalance)
            .GreaterThanOrEqualTo(0m)
            .When(x => x.CountedEWalletBalance.HasValue)
            .WithMessage("Counted e-wallet balance cannot be negative.");
    }
}
