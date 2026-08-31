using FluentValidation;

namespace POS.Application.PaymentMethods.Commands.CreatePaymentMethod;

public class CreatePaymentMethodCommandValidator : AbstractValidator<CreatePaymentMethodCommand>
{
    public CreatePaymentMethodCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(40).WithMessage("Name is too long.");

        RuleFor(x => x.Type).IsInEnum();
    }
}
