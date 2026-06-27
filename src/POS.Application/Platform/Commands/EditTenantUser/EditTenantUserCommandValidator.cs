using FluentValidation;

namespace POS.Application.Platform.Commands.EditTenantUser;

public class EditTenantUserCommandValidator : AbstractValidator<EditTenantUserCommand>
{
    public EditTenantUserCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password)
            .MinimumLength(8)
            .When(x => !string.IsNullOrEmpty(x.Password));
    }
}
