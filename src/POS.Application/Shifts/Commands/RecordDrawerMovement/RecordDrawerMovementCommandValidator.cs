using FluentValidation;

namespace POS.Application.Shifts.Commands.RecordDrawerMovement;

public class RecordDrawerMovementCommandValidator
    : AbstractValidator<RecordDrawerMovementCommand>
{
    public RecordDrawerMovementCommandValidator()
    {
        RuleFor(x => x.Amount)
            .NotEqual(0m).WithMessage("Amount cannot be zero.");

        RuleFor(x => x.Note)
            .NotEmpty().WithMessage("A note is required — record what the money was for.")
            .MaximumLength(256);
    }
}
