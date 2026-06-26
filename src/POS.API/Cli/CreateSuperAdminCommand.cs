using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Interfaces;
using POS.Domain.Entities;
using POS.Infrastructure.Persistence;

namespace POS.API.Cli;

public static class CreateSuperAdminCommand
{
    public const string Name = "create-admin";

    private const string DefaultEmail = "varquez.elmerjr@gmail.com";

    public static async Task<int> RunAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        Console.WriteLine("Create super admin account");
        Console.WriteLine("--------------------------");

        Console.Write($"Email [{DefaultEmail}]: ");
        var email = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(email))
            email = DefaultEmail;

        if (await db.Users.AnyAsync(u => u.Email == email, ct))
        {
            Console.Error.WriteLine($"A user with email '{email}' already exists. Aborting.");
            return 1;
        }

        var password = ReadSecret("Password: ");
        if (string.IsNullOrWhiteSpace(password))
        {
            Console.Error.WriteLine("Password cannot be empty. Aborting.");
            return 1;
        }

        if (!Console.IsInputRedirected)
        {
            var confirm = ReadSecret("Confirm password: ");
            if (!string.Equals(password, confirm, StringComparison.Ordinal))
            {
                Console.Error.WriteLine("Passwords do not match. Aborting.");
                return 1;
            }
        }

        db.Users.Add(new User
        {
            Name = "Super Admin",
            Email = email,
            PasswordHash = passwordHasher.Hash(password),
            Role = "SuperAdmin",
            IsActive = true
        });
        await db.SaveChangesAsync(ct);

        Console.WriteLine($"Super admin created: {email}");
        return 0;
    }

    private static string ReadSecret(string prompt)
    {
        Console.Write(prompt);

        if (Console.IsInputRedirected)
            return Console.ReadLine() ?? string.Empty;

        var secret = string.Empty;
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                break;
            }
            if (key.Key == ConsoleKey.Backspace)
            {
                if (secret.Length > 0)
                    secret = secret[..^1];
                continue;
            }
            if (!char.IsControl(key.KeyChar))
                secret += key.KeyChar;
        }
        return secret;
    }
}
