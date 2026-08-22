using FluentValidation;

namespace POS.Application.Utang.Commands.CollectUtangPayment;

public class CollectUtangPaymentCommandValidator
    : AbstractValidator<CollectUtangPaymentCommand>
{
    public CollectUtangPaymentCommandValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0m).WithMessage("Enter the amount collected.");
    }
}
