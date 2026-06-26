using FluentValidation;

namespace POS.Application.Cashiers.Commands.ResetCashierPassword;

public class ResetCashierPasswordCommandValidator : AbstractValidator<ResetCashierPasswordCommand>
{
    public ResetCashierPasswordCommandValidator()
    {
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}
