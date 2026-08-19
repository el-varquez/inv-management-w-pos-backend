using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Domain.Entities;
using POS.Infrastructure.Persistence;
using Xunit;

namespace POS.Infrastructure.Tests;

public class AdminSeederTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _ctx;

    public AdminSeederTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _ctx = new AppDbContext(options);
        _ctx.Database.EnsureCreated();
    }

    [Fact]
    public void Seeds_admin_when_no_users_exist()
    {
        AdminSeeder.Seed(_ctx);

        var user = Assert.Single(_ctx.Users.ToList());
        Assert.Equal("admin", user.Username);
        Assert.Equal("Admin", user.Name);
        Assert.Equal("Admin", user.Role);
        Assert.Null(user.PasswordHash);
        Assert.True(user.IsActive);
    }

    [Fact]
    public void Does_nothing_when_any_user_exists()
    {
        _ctx.Users.Add(new User
        {
            Name = "Nena",
            Username = "nena",
            Role = "Cashier",
            IsActive = false
        });
        _ctx.SaveChanges();

        AdminSeeder.Seed(_ctx);

        var user = Assert.Single(_ctx.Users.ToList());
        Assert.Equal("nena", user.Username);
    }

    [Fact]
    public void Seeding_twice_still_leaves_one_user()
    {
        AdminSeeder.Seed(_ctx);
        AdminSeeder.Seed(_ctx);

        Assert.Single(_ctx.Users.ToList());
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }
}
