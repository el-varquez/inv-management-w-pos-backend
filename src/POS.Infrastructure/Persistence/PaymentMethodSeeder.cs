using POS.Domain.Entities;
using POS.Domain.Enums;

namespace POS.Infrastructure.Persistence;

public static class PaymentMethodSeeder
{
    public static void Seed(AppDbContext db)
    {
        Ensure(db, PaymentMethodIds.Cash, "Cash", PaymentMethodType.Sales, false);
        Ensure(db, PaymentMethodIds.EWallet, "E-Wallet", PaymentMethodType.Sales, true);
        Ensure(db, PaymentMethodIds.Utang, "Utang", PaymentMethodType.Invoice, false);
        db.SaveChanges();
    }

    private static void Ensure(
        AppDbContext db, Guid id, string name, PaymentMethodType type, bool requiresReference)
    {
        if (db.PaymentMethods.Any(m => m.Id == id)) return;
        db.PaymentMethods.Add(new PaymentMethod
        {
            Id = id,
            Name = name,
            Type = type,
            RequiresReference = requiresReference,
            IsActive = true,
            IsSystem = true
        });
    }
}
