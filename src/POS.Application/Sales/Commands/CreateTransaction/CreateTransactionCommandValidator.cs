using FluentValidation;
using POS.Domain.Enums;

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

        RuleFor(x => x.ReferenceNumber)
            .MaximumLength(64).WithMessage("Reference number is too long.");

        RuleFor(x => x.SukiId)
            .NotNull().WithMessage("Pick a suki to charge.")
            .When(x => x.PaymentType == PaymentType.Utang);

        RuleFor(x => x.DownPayment)
            .GreaterThanOrEqualTo(0).WithMessage("Down payment cannot be negative.");
    }
}