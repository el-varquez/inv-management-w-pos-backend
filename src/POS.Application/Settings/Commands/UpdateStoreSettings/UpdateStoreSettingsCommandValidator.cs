using FluentValidation;

namespace POS.Application.Settings.Commands.UpdateStoreSettings;

public class UpdateStoreSettingsCommandValidator : AbstractValidator<UpdateStoreSettingsCommand>
{
    public UpdateStoreSettingsCommandValidator()
    {
        RuleFor(x => x.StoreName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.ReceiptFooter).MaximumLength(500);

        RuleFor(x => x.DefaultUtangMarkup)
            .GreaterThanOrEqualTo(0m)
            .WithMessage("Default utang markup must be 0 or greater.")
            .Must(HasAtMostTwoDecimalPlaces)
            .WithMessage("Default utang markup cannot have more than 2 decimal places.");
    }

    private static bool HasAtMostTwoDecimalPlaces(decimal value)
        => decimal.Round(value, 2) == value;
}
