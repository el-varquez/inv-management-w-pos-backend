using POS.Domain.Entities;

namespace POS.Application.Common;

public record UtangPrice(
    decimal BasePrice,
    decimal MarkupPerUnit,
    decimal UnitPrice,
    decimal LineTotal);

public static class UtangPricing
{
    public static UtangPrice Resolve(Item item, decimal defaultMarkup, int quantity)
    {
        var markupPerUnit = item.UtangMarkup ?? defaultMarkup;
        var unitPrice = item.SellingPrice + markupPerUnit;

        return new UtangPrice(
            item.SellingPrice,
            markupPerUnit,
            unitPrice,
            unitPrice * quantity);
    }
}
