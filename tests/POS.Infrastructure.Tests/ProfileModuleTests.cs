using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Application.Profile.Commands.ChangePassword;
using POS.Application.Profile.Commands.UpdateProfile;
using POS.Application.Profile.Queries.GetProfile;
using POS.Domain.Entities;
using POS.Domain.Exceptions;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;
using POS.Infrastructure.Services;
using POS.Infrastructure.Tests.Fakes;
using Xunit;

namespace POS.Infrastructure.Tests;

public class ProfileModuleTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _ctx;
    private readonly UserRepository _users;
    private readonly UnitOfWork _uow;
    private readonly PasswordHasher _hasher = new();

    public ProfileModuleTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _ctx = new AppDbContext(options);
        _ctx.Database.EnsureCreated();

        _users = new UserRepository(_ctx);
        _uow = new UnitOfWork(_ctx);
    }

    private async Task<User> SeedAdminAsync(string email = "owner@store.ph")
    {
        var admin = new User
        {
            Name = "Owner",
            Email = email,
            PasswordHash = _hasher.Hash("password123"),
            Role = "Admin",
            IsActive = true
        };
        await _users.AddAsync(admin);
        await _uow.SaveChangesAsync();
        return admin;
    }

    [Fact]
    public async Task GetProfile_returns_account()
    {
        var admin = await SeedAdminAsync();

        var handler = new GetProfileQueryHandler(
            new FakeCurrentUser { Id = admin.Id, Role = "Admin" }, _users);

        var result = await handler.Handle(new GetProfileQuery(), CancellationToken.None);

        Assert.Equal(admin.Id, result.Id);
        Assert.Equal("owner@store.ph", result.Email);
        Assert.Equal("Admin", result.Role);
    }

    private ChangePasswordCommandHandler ChangePasswordHandler(Guid userId) =>
        new(new FakeCurrentUser { Id = userId, Role = "Admin" }, _users, _uow, _hasher);

    [Fact]
    public async Task ChangePassword_with_wrong_current_is_rejected_and_hash_unchanged()
    {
        var admin = await SeedAdminAsync();
        var originalHash = admin.PasswordHash;

        await Assert.ThrowsAsync<DomainException>(() =>
            ChangePasswordHandler(admin.Id).Handle(
                new ChangePasswordCommand("wrongpassword", "newpassword123"),
                CancellationToken.None));

        var unchanged = await _ctx.Users.SingleAsync(u => u.Id == admin.Id);
        Assert.Equal(originalHash, unchanged.PasswordHash);
    }

    [Fact]
    public async Task ChangePassword_with_correct_current_rehashes()
    {
        var admin = await SeedAdminAsync();

        await ChangePasswordHandler(admin.Id).Handle(
            new ChangePasswordCommand("password123", "newpassword123"),
            CancellationToken.None);

        var updated = await _ctx.Users.SingleAsync(u => u.Id == admin.Id);
        Assert.True(_hasher.Verify("newpassword123", updated.PasswordHash!));
    }

    [Fact]
    public async Task ChangePassword_rejects_reusing_current_password()
    {
        var admin = await SeedAdminAsync();

        await Assert.ThrowsAsync<DomainException>(() =>
            ChangePasswordHandler(admin.Id).Handle(
                new ChangePasswordCommand("password123", "password123"),
                CancellationToken.None));
    }

    [Fact]
    public void ChangePassword_validator_rejects_short_new_password()
    {
        var result = new ChangePasswordCommandValidator()
            .Validate(new ChangePasswordCommand("password123", "short"));
        Assert.False(result.IsValid);
    }

    private UpdateProfileCommandHandler UpdateProfileHandler(Guid userId) =>
        new(new FakeCurrentUser { Id = userId, Role = "Admin" }, _users, _uow);

    [Fact]
    public async Task UpdateProfile_changes_name()
    {
        var admin = await SeedAdminAsync();

        await UpdateProfileHandler(admin.Id).Handle(
            new UpdateProfileCommand("Renamed Owner"),
            CancellationToken.None);

        var updated = await _ctx.Users.SingleAsync(u => u.Id == admin.Id);
        Assert.Equal("Renamed Owner", updated.Name);
    }

    [Fact]
    public void UpdateProfile_validator_rejects_blank_name()
    {
        var result = new UpdateProfileCommandValidator()
            .Validate(new UpdateProfileCommand("  "));
        Assert.False(result.IsValid);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }
}
