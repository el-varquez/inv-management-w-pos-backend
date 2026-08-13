using FluentValidation;

namespace POS.Application.Dashboard.Queries.GetSalesTrend;

public class GetSalesTrendQueryValidator : AbstractValidator<GetSalesTrendQuery>
{
    private static readonly string[] Allowed = { "day", "week", "month", "year" };

    public GetSalesTrendQueryValidator()
        => RuleFor(q => q.Period)
            .Must(p => p != null && Allowed.Contains(p.ToLowerInvariant()))
            .WithMessage("Period must be one of: day, week, month, year.");
}
