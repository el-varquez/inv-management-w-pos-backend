using FluentValidation;

namespace POS.Application.Invoices.Commands.CreateInvoice;

public class CreateInvoiceCommandValidator : AbstractValidator<CreateInvoiceCommand>
{
    public CreateInvoiceCommandValidator()
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

        RuleFor(x => x.InvoiceDiscount)
            .GreaterThanOrEqualTo(0).WithMessage("Discount cannot be negative.");

        RuleFor(x => x.SukiId)
            .NotEmpty().WithMessage("Pick a suki to charge.");
    }
}
