using POS.Domain.Entities;

namespace POS.Infrastructure.Persistence;

public static class CategorySeeder
{
    public static void Seed(AppDbContext db)
    {
        Ensure(db, CategoryNames.InventoryItem, "Physical products with tracked stock");
        Ensure(db, CategoryNames.Service, "Non-physical items — fees, deposits, services");
        db.SaveChanges();
    }

    private static void Ensure(AppDbContext db, string name, string description)
    {
        var existing = db.Categories.FirstOrDefault(c => c.Name == name);
        if (existing is null)
        {
            db.Categories.Add(new Category { Name = name, Description = description, IsSystem = true });
        }
        else if (!existing.IsSystem)
        {
            existing.IsSystem = true;
        }
    }
}
