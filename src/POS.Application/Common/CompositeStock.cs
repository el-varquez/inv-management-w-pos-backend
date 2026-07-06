using POS.Domain.Entities;

namespace POS.Application.Common;

public static class CompositeStock
{
    public static int Buildable(IEnumerable<CompositeItem> components)
    {
        var list = components.ToList();
        if (list.Count == 0) return 0;
        return list.Min(c => c.Quantity <= 0 ? 0 : (int)Math.Floor(c.ComponentItem.Stock / c.Quantity));
    }
}
