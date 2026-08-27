using FluentValidation;

namespace POS.Application.Utang.Commands.UpdateSuki;

public class UpdateSukiCommandValidator : AbstractValidator<UpdateSukiCommand>
{
    public UpdateSukiCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("The suki needs a name.")
            .MaximumLength(100);

        RuleFor(x => x.Phone)
            .MaximumLength(32);
    }
}
