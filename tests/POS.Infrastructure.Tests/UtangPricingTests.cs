using POS.Application.Common;
using POS.Domain.Entities;
using Xunit;

namespace POS.Infrastructure.Tests;

public class UtangPricingTests
{
    private static Item ItemAt(decimal sellingPrice, decimal? utangMarkup, bool isComposite = false)
        => new()
        {
            Name = "Test Item",
            SellingPrice = sellingPrice,
            UtangMarkup = utangMarkup,
            IsComposite = isComposite
        };

    [Fact]
    public void Null_markup_inherits_the_store_default()
    {
        var result = UtangPricing.Resolve(ItemAt(4m, null), defaultMarkup: 1m, quantity: 1);

        Assert.Equal(4m, result.BasePrice);
        Assert.Equal(1m, result.MarkupPerUnit);
        Assert.Equal(5m, result.UnitPrice);
        Assert.Equal(5m, result.LineTotal);
    }

    [Fact]
    public void Set_markup_overrides_the_store_default()
    {
        var result = UtangPricing.Resolve(ItemAt(200m, 10m), defaultMarkup: 1m, quantity: 1);

        Assert.Equal(10m, result.MarkupPerUnit);
        Assert.Equal(210m, result.UnitPrice);
    }

    [Fact]
    public void Zero_markup_means_no_markup_even_when_the_default_is_set()
    {
        var result = UtangPricing.Resolve(ItemAt(50m, 0m), defaultMarkup: 1m, quantity: 1);

        Assert.Equal(0m, result.MarkupPerUnit);
        Assert.Equal(50m, result.UnitPrice);
        Assert.Equal(50m, result.LineTotal);
    }

    [Fact]
    public void Quantity_multiplies_the_markup()
    {
        var result = UtangPricing.Resolve(ItemAt(4m, null), defaultMarkup: 1m, quantity: 3);

        Assert.Equal(5m, result.UnitPrice);
        Assert.Equal(15m, result.LineTotal);
    }

    [Fact]
    public void Composite_uses_its_own_markup_not_its_components()
    {
        var composite = ItemAt(35m, 2m, isComposite: true);

        var result = UtangPricing.Resolve(composite, defaultMarkup: 1m, quantity: 2);

        Assert.Equal(2m, result.MarkupPerUnit);
        Assert.Equal(37m, result.UnitPrice);
        Assert.Equal(74m, result.LineTotal);
    }
}
