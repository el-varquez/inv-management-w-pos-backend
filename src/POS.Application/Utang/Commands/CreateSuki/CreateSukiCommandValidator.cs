using FluentValidation;

namespace POS.Application.Utang.Commands.CreateSuki;

public class CreateSukiCommandValidator : AbstractValidator<CreateSukiCommand>
{
    public CreateSukiCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("The suki needs a name.")
            .MaximumLength(100);

        RuleFor(x => x.Phone)
            .MaximumLength(32);
    }
}
