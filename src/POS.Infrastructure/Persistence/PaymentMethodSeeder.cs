using POS.Domain.Entities;

namespace POS.Infrastructure.Persistence;

public static class PaymentMethodSeeder
{
    public static void Seed(AppDbContext db)
    {
        Ensure(db, PaymentMethodIds.Cash, "Cash", requiresReference: false);
        Ensure(db, PaymentMethodIds.GCash, "GCash", requiresReference: true);
        Ensure(db, PaymentMethodIds.Maya, "Maya", requiresReference: true);
        db.SaveChanges();
    }

    private static void Ensure(
        AppDbContext db, Guid id, string name, bool requiresReference)
    {
        if (db.PaymentMethods.Any(m => m.Id == id || m.Name == name)) return;
        db.PaymentMethods.Add(new PaymentMethod
        {
            Id = id,
            Name = name,
            RequiresReference = requiresReference,
            IsActive = true,
            IsSystem = true
        });
    }
}
