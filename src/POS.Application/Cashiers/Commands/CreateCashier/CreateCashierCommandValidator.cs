using FluentValidation;

namespace POS.Application.Cashiers.Commands.CreateCashier;

public class CreateCashierCommandValidator : AbstractValidator<CreateCashierCommand>
{
    public CreateCashierCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Username)
            .NotEmpty()
            .MaximumLength(64)
            .Matches("^[a-zA-Z0-9._-]+$")
            .WithMessage("Username may only contain letters, numbers, dots, underscores and hyphens.");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.Email)
            .EmailAddress()
            .MaximumLength(256)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}
