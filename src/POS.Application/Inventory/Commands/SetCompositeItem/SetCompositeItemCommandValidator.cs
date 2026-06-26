using FluentValidation;

namespace POS.Application.Inventory.Commands.SetCompositeItem;

public class SetCompositeItemCommandValidator : AbstractValidator<SetCompositeItemCommand>
{
    public SetCompositeItemCommandValidator()
    {
        RuleFor(x => x.Components)
            .NotNull().WithMessage("Components are required.");

        RuleForEach(x => x.Components).ChildRules(component =>
        {
            component.RuleFor(c => c.ComponentItemId)
                .NotEmpty().WithMessage("Component item is required.");

            component.RuleFor(c => c.Quantity)
                .GreaterThan(0).WithMessage("Component quantity must be greater than 0.");
        });

        RuleFor(x => x.Components)
            .Must(NoDuplicateComponents)
            .When(x => x.Components is not null)
            .WithMessage("A component cannot be listed more than once.");

        RuleFor(x => x)
            .Must(x => x.Components is null
                || x.Components.All(c => c.ComponentItemId != x.ParentItemId))
            .WithMessage("An item cannot be a component of itself.");
    }

    private static bool NoDuplicateComponents(IList<ComponentInput> components)
        => components.Select(c => c.ComponentItemId).Distinct().Count() == components.Count;
}
