using FluentValidation;

namespace POS.Application.Platform.Commands.SetCashierCap;

public class SetCashierCapCommandValidator : AbstractValidator<SetCashierCapCommand>
{
    public SetCashierCapCommandValidator()
    {
        RuleFor(x => x.CashierCap).GreaterThanOrEqualTo(1);
    }
}
