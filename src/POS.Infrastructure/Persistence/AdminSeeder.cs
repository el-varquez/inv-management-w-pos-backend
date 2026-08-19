using POS.Domain.Entities;

namespace POS.Infrastructure.Persistence;

public static class AdminSeeder
{
    public static void Seed(AppDbContext db)
    {
        if (db.Users.Any())
        {
            return;
        }
        db.Users.Add(new User
        {
            Name = "Admin",
            Username = "admin",
            Role = "Admin",
            PasswordHash = null,
            IsActive = true
        });
        db.SaveChanges();
    }
}
