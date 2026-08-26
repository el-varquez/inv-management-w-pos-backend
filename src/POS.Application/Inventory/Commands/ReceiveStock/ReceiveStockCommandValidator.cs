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
            .Must(lines => lines.Where(l => l.ItemId.HasValue)
                .Select(l => l.ItemId!.Value)
                .Distinct().Count() == lines.Count(l => l.ItemId.HasValue))
            .WithMessage("Each item can appear only once per delivery.")
            .Must(lines => lines.Where(l => l.NewItem is not null)
                .Select(l => (l.NewItem!.Name ?? string.Empty).Trim().ToLowerInvariant())
                .Distinct().Count() == lines.Count(l => l.NewItem is not null))
            .WithMessage("Each new item can appear only once per delivery.")
            .Must(lines => lines.Select(l => l.NewItem?.Barcode)
                .Where(b => !string.IsNullOrWhiteSpace(b))
                .Select(b => b!.Trim())
                .Distinct().Count()
                == lines.Count(l => !string.IsNullOrWhiteSpace(l.NewItem?.Barcode)))
            .WithMessage("Two new lines share the same barcode.")
            .When(x => x.Lines is { Count: > 0 });

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l)
                .Must(l => l.ItemId.HasValue != (l.NewItem is not null))
                .WithMessage("A line must carry exactly one of itemId or newItem.");
            line.RuleFor(l => l.Quantity)
                .GreaterThanOrEqualTo(1).WithMessage("Quantity must be at least 1.");
            line.RuleFor(l => l.CostPerUnit)
                .GreaterThanOrEqualTo(0).WithMessage("Cost per unit must be 0 or greater.");
            line.RuleFor(l => l.SellingPrice)
                .GreaterThan(0).WithMessage("Selling price must be greater than 0.");
            line.When(l => l.NewItem is not null, () =>
            {
                line.RuleFor(l => l.NewItem!.Name)
                    .NotEmpty().WithMessage("Item name is required.")
                    .MaximumLength(100);
                line.RuleFor(l => l.NewItem!.Barcode)
                    .MaximumLength(64).WithMessage("Barcode must be 64 characters or fewer.");
            });
        });

        RuleFor(x => x.SupplierName).MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
