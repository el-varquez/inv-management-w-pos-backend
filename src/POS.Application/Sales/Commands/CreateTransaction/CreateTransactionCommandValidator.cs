using FluentValidation;

namespace POS.Application.Sales.Commands.CreateTransaction;

public class CreateTransactionCommandValidator : AbstractValidator<CreateTransactionCommand>
{
    public CreateTransactionCommandValidator()
    {
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Cart cannot be empty.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than 0.");
            item.RuleFor(i => i.Discount)
                .GreaterThanOrEqualTo(0).WithMessage("Discount cannot be negative.");
        });

        RuleFor(x => x.TransactionDiscount)
            .GreaterThanOrEqualTo(0).WithMessage("Discount cannot be negative.");

        RuleFor(x => x.AmountTendered)
            .GreaterThanOrEqualTo(0).WithMessage("Amount tendered cannot be negative.");
    }
}