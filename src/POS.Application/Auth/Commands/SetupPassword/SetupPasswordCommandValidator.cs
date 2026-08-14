using FluentValidation;

namespace POS.Application.Auth.Commands.SetupPassword;

public class SetupPasswordCommandValidator : AbstractValidator<SetupPasswordCommand>
{
    public SetupPasswordCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(64);
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
    }
}
