using FluentValidation;

namespace POS.Application.Inventory.Commands.ReceiveStock;

public class ReceiveStockCommandValidator : AbstractValidator<ReceiveStockCommand>
{
    public ReceiveStockCommandValidator()
    {
        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("A delivery needs at least one line.");

        RuleFor(x => x.Lines)
            .Must(lines => lines.Count <= 200)
            .WithMessage("A delivery can have at most 200 lines.")
            .Must(lines => lines.Select(l => l.ItemId).Distinct().Count() == lines.Count)
            .WithMessage("Each item can appear only once per delivery.")
            .When(x => x.Lines is { Count: > 0 });

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.Quantity)
                .GreaterThanOrEqualTo(1).WithMessage("Quantity must be at least 1.");
            line.RuleFor(l => l.CostPerUnit)
                .GreaterThanOrEqualTo(0).WithMessage("Cost per unit must be 0 or greater.");
            line.RuleFor(l => l.SellingPrice)
                .GreaterThan(0).WithMessage("Selling price must be greater than 0.");
        });

        RuleFor(x => x.SupplierName).MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
