using FluentValidation;
using POS.Domain.Enums;

namespace POS.Application.Shifts.Commands.RecordEWalletTransaction;

public class RecordEWalletTransactionCommandValidator
    : AbstractValidator<RecordEWalletTransactionCommand>
{
    public RecordEWalletTransactionCommandValidator()
    {
        RuleFor(x => x.Direction)
            .Must(d => d is EWalletDirection.CashIn or EWalletDirection.CashOut)
            .WithMessage("Only cash in and cash out are supported.");

        RuleFor(x => x.Principal)
            .GreaterThan(0m).WithMessage("Enter the amount.");

        RuleFor(x => x.Fee)
            .GreaterThanOrEqualTo(0m).WithMessage("The fee cannot be negative.")
            .Must(f => f == decimal.Truncate(f))
                .WithMessage("The fee must be a whole peso amount.");

        RuleFor(x => x)
            .Must(x => x.Fee < x.Principal)
            .WithMessage("The fee cannot be as large as the amount.")
            .When(x => x.Principal > 0m);
    }
}
