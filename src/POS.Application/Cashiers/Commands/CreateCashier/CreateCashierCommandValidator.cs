using FluentValidation;

namespace POS.Application.Cashiers.Commands.CreateCashier;

public class CreateCashierCommandValidator : AbstractValidator<CreateCashierCommand>
{
    public CreateCashierCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}
