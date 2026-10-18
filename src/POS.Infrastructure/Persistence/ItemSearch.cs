using POS.Domain.Entities;

namespace POS.Infrastructure.Persistence;

/// <summary>
/// The one search rule for every items and inventory list, applied in SQL: a blank term is no
/// filter; otherwise an exact barcode, or a case-insensitive substring of the name or item code.
/// Movement and count-line queries apply the same clauses through their Item.
/// </summary>
internal static class ItemSearch
{
    public static string? Normalize(string? search)
    {
        var trimmed = search?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    public static IQueryable<Item> WhereMatches(this IQueryable<Item> items, string? search)
    {
        var term = Normalize(search);
        if (term is null) return items;
        var lowered = term.ToLower();
        return items.Where(i =>
            i.Barcode == term ||
            i.Name.ToLower().Contains(lowered) ||
            i.ItemCode.ToLower().Contains(lowered));
    }

    public static IQueryable<StockMovement> WhereItemMatches(this IQueryable<StockMovement> movements, string? search)
    {
        var term = Normalize(search);
        if (term is null) return movements;
        var lowered = term.ToLower();
        return movements.Where(m =>
            m.Item.Barcode == term ||
            m.Item.Name.ToLower().Contains(lowered) ||
            m.Item.ItemCode.ToLower().Contains(lowered));
    }

    public static IQueryable<InventoryCountLine> WhereItemMatches(this IQueryable<InventoryCountLine> lines, string? search)
    {
        var term = Normalize(search);
        if (term is null) return lines;
        var lowered = term.ToLower();
        return lines.Where(l =>
            l.Item.Barcode == term ||
            l.Item.Name.ToLower().Contains(lowered) ||
            l.Item.ItemCode.ToLower().Contains(lowered));
    }
}
