using FluentValidation;

namespace POS.Application.Days.Commands.ReopenDay;

public class ReopenDayCommandValidator : AbstractValidator<ReopenDayCommand>
{
    public ReopenDayCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(256);
    }
}
