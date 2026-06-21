# Multi-Tenancy Slice A — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Convert the single-tenant POS into a multi-tenant SaaS foundation: a `Tenant` entity, leak-proof tenant isolation (EF global query filter + auto-stamp on insert), a `tenant_id` JWT claim, a three-role hierarchy, and a public self-serve registration endpoint + screen.

**Architecture:** Every business entity implements `ITenantScoped` (non-nullable `Guid TenantId`). A global EF Core query filter scopes all reads to the current tenant; a `SaveChangesAsync` override auto-stamps `TenantId` on insert. The current tenant is resolved from a new `tenant_id` JWT claim via `ICurrentUser.TenantId`, injected into `AppDbContext`. `User` is **not** tenant-scoped (it carries a *nullable* `Guid? TenantId` and is queried globally at login, since emails are globally unique). `SuperAdmin` has `TenantId = null` and therefore sees nothing through normal repos (default-deny). Registration creates `Tenant` + `Admin` `User` atomically and returns a JWT.

**Tech Stack:** .NET / C# (Clean Architecture: Domain ← Application ← Infrastructure ← API), EF Core + Npgsql, MediatR CQRS + FluentValidation, JWT bearer, BCrypt. Frontend: React + TypeScript + Vite + Zustand + Axios + react-router-dom. Tests: xUnit + EF Core SQLite in-memory (new test project).

## Global Constraints

- **Run all backend commands from the git root `POS/`.** Solution builds with `dotnet build`.
- **EF migration paths:** `--project backend/src/POS.Infrastructure --startup-project backend/src/POS.API` (NOT `src/POS.*`).
- **Namespace convention is folder-based** (`POS.Domain.Entities`, `POS.Application.Common.Interfaces`, …) **except** domain exceptions which live under `POS.Domain.Exceptions` (in the `Interfaces/` folder). Throw `DomainException` (→ HTTP 400) and `NotFoundException` (→ 404); both are in `POS.Domain.Exceptions`.
- **Enums bind/serialize as names** (a `JsonStringEnumConverter` is registered globally).
- **Three roles, stored as plain strings on `User.Role`:** `"SuperAdmin"`, `"Admin"`, `"Cashier"`. `SuperAdmin` → `TenantId = null`. `[Authorize(Roles = "Admin")]` on existing controllers means *business* admin — do not put `SuperAdmin` on those surfaces.
- **Frontend conventions:** feature-folder layout (`features/<module>/{services,hooks,screens,components}`); data flows service → hook → screen; unwrap errors with `getApiErrorMessage(err, fallback)`; use `import type` for type-only imports; reuse existing CSS classes from `index.css` (`.login-*`, `.field`, `.input`, `.btn`, …) — no CSS framework. Frontend must pass `npm run build` and `npm run lint`.
- **Dev DB is reset, not backfilled.** No production data exists.

---

## File Structure

**Domain (`backend/src/POS.Domain/`)**
- `Common/ITenantScoped.cs` — *create*: marker interface with `Guid TenantId`.
- `Entities/Tenant.cs` — *create*: the business-identity aggregate root (`Name`, `CashierCap`).
- `Entities/User.cs` — *modify*: add nullable `Guid? TenantId`.
- `Entities/{Category,Item,StockMovement,Transaction,TransactionItem,CompositeItem,InventoryCount,InventoryCountLine}.cs` — *modify*: implement `ITenantScoped`.
- `Interfaces/ITenantRepository.cs` — *create*: `AddAsync(Tenant)`.

**Application (`backend/src/POS.Application/`)**
- `Common/Interfaces/ICurrentUser.cs` — *modify*: add `Guid? TenantId`.
- `Auth/Commands/Register/RegisterCommand.cs` — *create*: command + `RegisterResult`.
- `Auth/Commands/Register/RegisterCommandHandler.cs` — *create*: atomic tenant+admin creation.
- `Auth/Commands/Register/RegisterCommandValidator.cs` — *create*: FluentValidation.

**Infrastructure (`backend/src/POS.Infrastructure/`)**
- `Services/JwtService.cs` — *modify*: emit `tenant_id` claim.
- `Persistence/AppDbContext.cs` — *modify*: inject `ICurrentUser`, register `Tenant`, global query filters, auto-stamp.
- `Persistence/Repositories/TenantRepository.cs` — *create*.
- `DependencyInjection.cs` — *modify*: register `ITenantRepository`.
- `Migrations/*` — *create* via EF: `MultiTenancy` migration.

**API (`backend/src/POS.API/`)**
- `Services/CurrentUser.cs` — *modify*: read `tenant_id` claim.
- `Controllers/AuthController.cs` — *modify*: add anonymous `POST /api/auth/register`.
- `Cli/CreateSuperAdminCommand.cs` — *modify*: stamp `Role = "SuperAdmin"`.

**Tests (`backend/tests/`)**
- `POS.Infrastructure.Tests/POS.Infrastructure.Tests.csproj` — *create*.
- `POS.Infrastructure.Tests/Fakes/FakeCurrentUser.cs` — *create*.
- `POS.Infrastructure.Tests/TenantIsolationTests.cs` — *create*.
- `POS.Infrastructure.Tests/RegistrationTests.cs` — *create*.

**Frontend (`frontend/src/`)**
- `types/index.ts` — *modify*: add `RegisterPayload`.
- `features/auth/services/authService.ts` — *modify*: add `register`.
- `features/auth/hooks/useAuth.ts` — *modify*: add `register`.
- `features/auth/screens/RegisterScreen.tsx` — *create*.
- `features/auth/screens/LoginScreen.tsx` — *modify*: add "Create an account" link.
- `App.tsx` — *modify*: add `/register` route.

---

## Task 1: `Tenant` entity + `ITenantScoped` marker

**Files:**
- Create: `backend/src/POS.Domain/Common/ITenantScoped.cs`
- Create: `backend/src/POS.Domain/Entities/Tenant.cs`

**Interfaces:**
- Produces: `interface ITenantScoped { Guid TenantId { get; set; } }` in namespace `POS.Domain.Common`; `class Tenant : BaseEntity` with `string Name`, `int CashierCap` (default 5) in `POS.Domain.Entities`. `Tenant` does **not** implement `ITenantScoped` (it is the tenant root).

- [ ] **Step 1: Create the marker interface**

`backend/src/POS.Domain/Common/ITenantScoped.cs`:

```csharp
namespace POS.Domain.Common;

/// <summary>
/// Marks an entity as owned by a single tenant. Every implementer is
/// automatically scoped by the EF global query filter (reads) and
/// auto-stamped with the current tenant on insert (writes).
/// </summary>
public interface ITenantScoped
{
    Guid TenantId { get; set; }
}
```

- [ ] **Step 2: Create the `Tenant` entity**

`backend/src/POS.Domain/Entities/Tenant.cs`:

```csharp
using POS.Domain.Common;

namespace POS.Domain.Entities;

public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Max active cashiers allowed for this tenant (first plan limit).</summary>
    public int CashierCap { get; set; } = 5;
}
```

- [ ] **Step 3: Build to verify it compiles**

Run: `dotnet build`
Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Commit**

```bash
git add backend/src/POS.Domain/Common/ITenantScoped.cs backend/src/POS.Domain/Entities/Tenant.cs
git commit -m "feat: add Tenant entity and ITenantScoped marker"
```

---

## Task 2: Add `TenantId` to `User` and all business entities

**Files:**
- Modify: `backend/src/POS.Domain/Entities/User.cs`
- Modify: `backend/src/POS.Domain/Entities/Category.cs`
- Modify: `backend/src/POS.Domain/Entities/Item.cs`
- Modify: `backend/src/POS.Domain/Entities/StockMovement.cs`
- Modify: `backend/src/POS.Domain/Entities/Transaction.cs`
- Modify: `backend/src/POS.Domain/Entities/TransactionItem.cs`
- Modify: `backend/src/POS.Domain/Entities/CompositeItem.cs`
- Modify: `backend/src/POS.Domain/Entities/InventoryCount.cs`
- Modify: `backend/src/POS.Domain/Entities/InventoryCountLine.cs`

**Interfaces:**
- Consumes: `ITenantScoped` from Task 1.
- Produces: `User.TenantId` is `Guid?` (nullable; `SuperAdmin` = null). The 8 business entities each implement `ITenantScoped` with non-nullable `Guid TenantId`.

- [ ] **Step 1: Add nullable `TenantId` to `User`** (User is NOT `ITenantScoped` — it is queried globally at login)

In `backend/src/POS.Domain/Entities/User.cs`, add after the `IsActive` property:

```csharp
    /// <summary>Owning tenant. Null only for the platform SuperAdmin.</summary>
    public Guid? TenantId { get; set; }
```

- [ ] **Step 2: Make `Category` tenant-scoped**

In `backend/src/POS.Domain/Entities/Category.cs`, change the class declaration and add the property:

```csharp
public class Category : BaseEntity, ITenantScoped
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<Item> Items { get; set; }= new List<Item>();
}
```

Add `using POS.Domain.Common;` if not already present (it is).

- [ ] **Step 3: Make the remaining 7 business entities tenant-scoped**

For each of `Item`, `StockMovement`, `Transaction`, `TransactionItem`, `CompositeItem`, `InventoryCount`, `InventoryCountLine`:
1. Add `, ITenantScoped` to the class declaration (e.g. `public class Item : BaseEntity, ITenantScoped`).
2. Add `public Guid TenantId { get; set; }` as the first property after the opening brace.

`ITenantScoped` lives in `POS.Domain.Common`, which every entity already imports via `using POS.Domain.Common;`. Each entity file already has that using — verify and add only if missing.

- [ ] **Step 4: Build to verify it compiles**

Run: `dotnet build`
Expected: Build succeeded, 0 errors. (Repositories need no changes — the query filter added in Task 5 does the scoping.)

- [ ] **Step 5: Commit**

```bash
git add backend/src/POS.Domain/Entities
git commit -m "feat: add TenantId to User and tenant-scope business entities"
```

---

## Task 3: Extend `ICurrentUser` and `CurrentUser` with `TenantId`

**Files:**
- Modify: `backend/src/POS.Application/Common/Interfaces/ICurrentUser.cs`
- Modify: `backend/src/POS.API/Services/CurrentUser.cs`

**Interfaces:**
- Produces: `ICurrentUser.TenantId` of type `Guid?`. `CurrentUser` reads it from a `"tenant_id"` JWT claim; returns `null` when the claim is absent or unparseable (anonymous requests, `SuperAdmin`).

- [ ] **Step 1: Add `TenantId` to the interface**

Replace the body of `backend/src/POS.Application/Common/Interfaces/ICurrentUser.cs`:

```csharp
namespace POS.Application.Common.Interfaces;

public interface ICurrentUser
{
    Guid Id { get; }
    string Role { get; }

    /// <summary>Current tenant from the JWT. Null for SuperAdmin / anonymous.</summary>
    Guid? TenantId { get; }
}
```

- [ ] **Step 2: Implement it in `CurrentUser`**

Replace the body of `backend/src/POS.API/Services/CurrentUser.cs`:

```csharp
using System.Security.Claims;
using POS.Application.Common.Interfaces;

namespace POS.API.Services;

public class CurrentUser : ICurrentUser
{
    public const string TenantClaimType = "tenant_id";

    private readonly IHttpContextAccessor _accessor;
    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    public Guid Id => Guid.TryParse(_accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

    public string Role => _accessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    public Guid? TenantId =>
        Guid.TryParse(_accessor.HttpContext?.User.FindFirstValue(TenantClaimType), out var tid)
            ? tid
            : null;
}
```

- [ ] **Step 3: Build to verify it compiles**

Run: `dotnet build`
Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Commit**

```bash
git add backend/src/POS.Application/Common/Interfaces/ICurrentUser.cs backend/src/POS.API/Services/CurrentUser.cs
git commit -m "feat: expose TenantId on ICurrentUser from tenant_id claim"
```

---

## Task 4: Emit `tenant_id` claim in JWT

**Files:**
- Modify: `backend/src/POS.Infrastructure/Services/JwtService.cs`

**Interfaces:**
- Consumes: `User.TenantId` (`Guid?`, Task 2). The claim type string `"tenant_id"` must match `CurrentUser.TenantClaimType` (Task 3).

- [ ] **Step 1: Add the `tenant_id` claim conditionally**

In `backend/src/POS.Infrastructure/Services/JwtService.cs`, replace the `var claims = new[] { ... };` block with a list that appends the tenant claim only when the user has a tenant:

```csharp
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role)
        };

        if (user.TenantId is Guid tenantId)
            claims.Add(new Claim("tenant_id", tenantId.ToString()));
```

The `JwtSecurityToken(... claims: claims ...)` call accepts the `List<Claim>` unchanged.

- [ ] **Step 2: Build to verify it compiles**

Run: `dotnet build`
Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```bash
git add backend/src/POS.Infrastructure/Services/JwtService.cs
git commit -m "feat: include tenant_id claim in issued JWTs"
```

---

## Task 5: Isolation core — query filter + auto-stamp in `AppDbContext` (TDD)

This is the highest-risk task: it makes cross-tenant leaks impossible by construction. It is built test-first against a real EF Core context (SQLite in-memory).

**Files:**
- Create: `backend/tests/POS.Infrastructure.Tests/POS.Infrastructure.Tests.csproj`
- Create: `backend/tests/POS.Infrastructure.Tests/Fakes/FakeCurrentUser.cs`
- Create: `backend/tests/POS.Infrastructure.Tests/TenantIsolationTests.cs`
- Modify: `backend/src/POS.Infrastructure/Persistence/AppDbContext.cs`

**Interfaces:**
- Consumes: `ICurrentUser.TenantId` (Task 3), `ITenantScoped` (Task 1), `Tenant` (Task 1), tenant-scoped entities (Task 2).
- Produces: `AppDbContext` constructor `(DbContextOptions<AppDbContext> options, ICurrentUser currentUser, IMediator? mediator = null)`; `DbSet<Tenant> Tenants`; a global query filter on every `ITenantScoped` entity; `SaveChangesAsync` auto-stamps `TenantId` on added `ITenantScoped` entities and throws `InvalidOperationException` if the current tenant is null.

- [ ] **Step 1: Scaffold the test project and add it to the solution**

Run from `POS/`:

```bash
dotnet new xunit -o backend/tests/POS.Infrastructure.Tests
dotnet sln add backend/tests/POS.Infrastructure.Tests/POS.Infrastructure.Tests.csproj
dotnet add backend/tests/POS.Infrastructure.Tests reference backend/src/POS.Infrastructure/POS.Infrastructure.csproj
dotnet add backend/tests/POS.Infrastructure.Tests reference backend/src/POS.Application/POS.Application.csproj
dotnet add backend/tests/POS.Infrastructure.Tests reference backend/src/POS.Domain/POS.Domain.csproj
dotnet add backend/tests/POS.Infrastructure.Tests package Microsoft.EntityFrameworkCore.Sqlite
```

- [ ] **Step 2: Create the fake current user**

`backend/tests/POS.Infrastructure.Tests/Fakes/FakeCurrentUser.cs`:

```csharp
using POS.Application.Common.Interfaces;

namespace POS.Infrastructure.Tests.Fakes;

public class FakeCurrentUser : ICurrentUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Role { get; set; } = "Admin";
    public Guid? TenantId { get; set; }
}
```

- [ ] **Step 3: Write the failing isolation tests**

`backend/tests/POS.Infrastructure.Tests/TenantIsolationTests.cs`:

```csharp
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Domain.Entities;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Tests.Fakes;
using Xunit;

namespace POS.Infrastructure.Tests;

public class TenantIsolationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public TenantIsolationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        // Create schema once using a context with no tenant.
        using var ctx = new AppDbContext(_options, new FakeCurrentUser { TenantId = null });
        ctx.Database.EnsureCreated();
    }

    private AppDbContext ContextFor(Guid? tenantId) =>
        new AppDbContext(_options, new FakeCurrentUser { TenantId = tenantId });

    [Fact]
    public async Task Insert_auto_stamps_current_tenant()
    {
        var tenantA = Guid.NewGuid();
        await using var ctx = ContextFor(tenantA);

        var category = new Category { Name = "Drinks" }; // TenantId left default
        ctx.Categories.Add(category);
        await ctx.SaveChangesAsync();

        Assert.Equal(tenantA, category.TenantId);
    }

    [Fact]
    public async Task Query_returns_only_current_tenant_rows()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        await using (var ctxA = ContextFor(tenantA))
        {
            ctxA.Categories.Add(new Category { Name = "A-Drinks" });
            await ctxA.SaveChangesAsync();
        }
        await using (var ctxB = ContextFor(tenantB))
        {
            ctxB.Categories.Add(new Category { Name = "B-Snacks" });
            await ctxB.SaveChangesAsync();
        }

        await using var readA = ContextFor(tenantA);
        var namesA = await readA.Categories.Select(c => c.Name).ToListAsync();

        Assert.Equal(new[] { "A-Drinks" }, namesA);
    }

    [Fact]
    public async Task SuperAdmin_with_null_tenant_sees_nothing()
    {
        var tenantA = Guid.NewGuid();
        await using (var ctxA = ContextFor(tenantA))
        {
            ctxA.Categories.Add(new Category { Name = "A-Drinks" });
            await ctxA.SaveChangesAsync();
        }

        await using var superAdmin = ContextFor(null);
        var count = await superAdmin.Categories.CountAsync();

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Insert_without_tenant_context_throws()
    {
        await using var ctx = ContextFor(null);
        ctx.Categories.Add(new Category { Name = "Orphan" });

        await Assert.ThrowsAsync<InvalidOperationException>(() => ctx.SaveChangesAsync());
    }

    public void Dispose() => _connection.Dispose();
}
```

- [ ] **Step 4: Run the tests to verify they fail**

Run: `dotnet test backend/tests/POS.Infrastructure.Tests`
Expected: Compile error or FAIL — `AppDbContext` has no constructor taking `ICurrentUser`, no query filter, no auto-stamp.

- [ ] **Step 5: Implement the isolation core in `AppDbContext`**

Edit `backend/src/POS.Infrastructure/Persistence/AppDbContext.cs`:

(a) Add usings at the top (keep existing ones):

```csharp
using POS.Application.Common.Interfaces;
```

(b) Replace the field + constructor region:

```csharp
    private readonly IMediator? _mediator;
    private readonly ICurrentUser? _currentUser;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        ICurrentUser currentUser,
        IMediator? mediator = null)
        : base(options)
    {
        _currentUser = currentUser;
        _mediator = mediator;
    }
```

(c) Register the `Tenant` DbSet alongside the existing `DbSet`s:

```csharp
    public DbSet<Tenant> Tenants => Set<Tenant>();
```

(d) At the **end** of `OnModelCreating` (after the last existing config line, before the closing brace), add the global query filters. List every tenant-scoped entity explicitly so the filter is unmistakable:

```csharp
        builder.Entity<Category>().HasQueryFilter(e => e.TenantId == _currentUser!.TenantId);
        builder.Entity<Item>().HasQueryFilter(e => e.TenantId == _currentUser!.TenantId);
        builder.Entity<StockMovement>().HasQueryFilter(e => e.TenantId == _currentUser!.TenantId);
        builder.Entity<Transaction>().HasQueryFilter(e => e.TenantId == _currentUser!.TenantId);
        builder.Entity<TransactionItem>().HasQueryFilter(e => e.TenantId == _currentUser!.TenantId);
        builder.Entity<CompositeItem>().HasQueryFilter(e => e.TenantId == _currentUser!.TenantId);
        builder.Entity<InventoryCount>().HasQueryFilter(e => e.TenantId == _currentUser!.TenantId);
        builder.Entity<InventoryCountLine>().HasQueryFilter(e => e.TenantId == _currentUser!.TenantId);
```

> Note: the filter captures `this` and evaluates `_currentUser.TenantId` at query time. `AppDbContext` and `CurrentUser` are both scoped, so the tenant is fixed per request. `_currentUser!` is non-null in all real and test paths (DI always provides it).

(e) In `SaveChangesAsync`, auto-stamp **before** the existing domain-events logic. Insert this at the very start of the method (before `var entitiesWithEvents = ...`):

```csharp
        foreach (var entry in ChangeTracker.Entries<ITenantScoped>()
                     .Where(e => e.State == EntityState.Added))
        {
            if (_currentUser?.TenantId is not Guid tenantId)
                throw new InvalidOperationException(
                    "Cannot persist a tenant-scoped entity without a tenant context.");
            entry.Entity.TenantId = tenantId;
        }
```

Add `using POS.Domain.Common;` if not already present (it is, for `BaseEntity`).

- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet test backend/tests/POS.Infrastructure.Tests`
Expected: PASS — all 4 tests green.

- [ ] **Step 7: Build the whole solution**

Run: `dotnet build`
Expected: Build succeeded, 0 errors. (EF may emit query-filter relationship warnings; they are acceptable since every related entity carries the same filter.)

- [ ] **Step 8: Commit**

```bash
git add backend/tests/POS.Infrastructure.Tests backend/src/POS.Infrastructure/Persistence/AppDbContext.cs POS.sln
git commit -m "feat: enforce tenant isolation via query filter and auto-stamp"
```

---

## Task 6: `MultiTenancy` migration + dev DB reset

**Files:**
- Create (via EF): `backend/src/POS.Infrastructure/Migrations/<timestamp>_MultiTenancy.cs` (+ `.Designer.cs`, snapshot update).

**Interfaces:**
- Consumes: schema changes from Tasks 1, 2, 5 (`Tenants` table, `TenantId` columns).

- [ ] **Step 1: Add the migration**

Run from `POS/`:

```bash
dotnet ef migrations add MultiTenancy --project backend/src/POS.Infrastructure --startup-project backend/src/POS.API
```

Expected: a new migration is created adding the `Tenants` table and `TenantId` columns on `Users` + the 8 business tables.

- [ ] **Step 2: Inspect the generated migration**

Read `backend/src/POS.Infrastructure/Migrations/<timestamp>_MultiTenancy.cs`. Confirm:
- `Tenants` table created with `Name`, `CashierCap`, `Id`, `CreatedAt`, `UpdatedAt`.
- `TenantId` added to `Users` as nullable (`Guid?`).
- `TenantId` added to the 8 business tables as non-nullable `Guid`.

No code edits expected; this step is verification only.

- [ ] **Step 3: Reset and recreate the dev DB**

Ensure `appsettings.Development.json` (or `.env`) points at the dev Postgres DB, then run from `POS/`:

```bash
dotnet ef database drop --force --project backend/src/POS.Infrastructure --startup-project backend/src/POS.API
dotnet ef database update --project backend/src/POS.Infrastructure --startup-project backend/src/POS.API
```

Expected: DB dropped, then all migrations (including `MultiTenancy`) apply cleanly. Final schema has `TenantId` on every business table.

- [ ] **Step 4: Commit**

```bash
git add backend/src/POS.Infrastructure/Migrations
git commit -m "feat: add MultiTenancy migration"
```

---

## Task 7: Promote `create-admin` CLI to `SuperAdmin`

**Files:**
- Modify: `backend/src/POS.API/Cli/CreateSuperAdminCommand.cs`

**Interfaces:**
- Consumes: `User` with nullable `TenantId` (Task 2). The created user has `Role = "SuperAdmin"` and `TenantId = null` (left unset → null).

- [ ] **Step 1: Change the seeded role**

In `backend/src/POS.API/Cli/CreateSuperAdminCommand.cs`, in the `db.Users.Add(new User { ... })` block, change:

```csharp
            Role = "Admin",
```

to:

```csharp
            Role = "SuperAdmin",
```

Leave `TenantId` unset (defaults to `null`, which is correct for a SuperAdmin).

- [ ] **Step 2: Build and create the SuperAdmin account**

Run from `POS/`:

```bash
dotnet build
dotnet run --project backend/src/POS.API -- create-admin
```

Enter the owner email + a password when prompted. Expected: `Super admin created: <email>`.

- [ ] **Step 3: Commit**

```bash
git add backend/src/POS.API/Cli/CreateSuperAdminCommand.cs
git commit -m "feat: create-admin CLI stamps SuperAdmin role"
```

---

## Task 8: Registration command + endpoint (TDD)

**Files:**
- Create: `backend/src/POS.Domain/Interfaces/ITenantRepository.cs`
- Create: `backend/src/POS.Infrastructure/Persistence/Repositories/TenantRepository.cs`
- Modify: `backend/src/POS.Infrastructure/DependencyInjection.cs`
- Create: `backend/src/POS.Application/Auth/Commands/Register/RegisterCommand.cs`
- Create: `backend/src/POS.Application/Auth/Commands/Register/RegisterCommandValidator.cs`
- Create: `backend/src/POS.Application/Auth/Commands/Register/RegisterCommandHandler.cs`
- Modify: `backend/src/POS.API/Controllers/AuthController.cs`
- Create: `backend/tests/POS.Infrastructure.Tests/RegistrationTests.cs`

**Interfaces:**
- Consumes: `IUserRepository.AddAsync` + `GetByEmailAsync`, `IUnitOfWork`, `IJwtService.GenerateToken(User)`, `IPasswordHasher.Hash`, `Tenant`, `User`, `DomainException`.
- Produces:
  - `interface ITenantRepository { Task AddAsync(Tenant tenant, CancellationToken ct = default); }`
  - `record RegisterCommand(string BusinessName, string AdminName, string Email, string Password) : IRequest<RegisterResult>`
  - `record RegisterResult(string Token, string Name, string Email, string Role)`

- [ ] **Step 1: Create the tenant repository interface**

`backend/src/POS.Domain/Interfaces/ITenantRepository.cs`:

```csharp
using POS.Domain.Entities;

namespace POS.Domain.Interfaces;

public interface ITenantRepository
{
    Task AddAsync(Tenant tenant, CancellationToken ct = default);
}
```

- [ ] **Step 2: Implement and register the tenant repository**

`backend/src/POS.Infrastructure/Persistence/Repositories/TenantRepository.cs`:

```csharp
using POS.Domain.Entities;
using POS.Domain.Interfaces;

namespace POS.Infrastructure.Persistence.Repositories;

public class TenantRepository : ITenantRepository
{
    private readonly AppDbContext _context;

    public TenantRepository(AppDbContext context) => _context = context;

    public async Task AddAsync(Tenant tenant, CancellationToken ct = default)
        => await _context.Tenants.AddAsync(tenant, ct);
}
```

In `backend/src/POS.Infrastructure/DependencyInjection.cs`, add alongside the other `AddScoped` repository registrations:

```csharp
        services.AddScoped<ITenantRepository, TenantRepository>();
```

- [ ] **Step 3: Create the command + result**

`backend/src/POS.Application/Auth/Commands/Register/RegisterCommand.cs`:

```csharp
using MediatR;

namespace POS.Application.Auth.Commands.Register;

public record RegisterCommand(
    string BusinessName,
    string AdminName,
    string Email,
    string Password
) : IRequest<RegisterResult>;

public record RegisterResult(
    string Token,
    string Name,
    string Email,
    string Role
);
```

- [ ] **Step 4: Create the validator**

`backend/src/POS.Application/Auth/Commands/Register/RegisterCommandValidator.cs`:

```csharp
using FluentValidation;

namespace POS.Application.Auth.Commands.Register;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.BusinessName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AdminName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}
```

- [ ] **Step 5: Create the handler**

`backend/src/POS.Application/Auth/Commands/Register/RegisterCommandHandler.cs`:

```csharp
using MediatR;
using POS.Application.Common.Interfaces;
using POS.Domain.Entities;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Auth.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResult>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterCommandHandler(
        ITenantRepository tenantRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IJwtService jwtService,
        IPasswordHasher passwordHasher)
    {
        _tenantRepository = tenantRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
    }

    public async Task<RegisterResult> Handle(RegisterCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLower();

        if (await _userRepository.GetByEmailAsync(email, ct) is not null)
            throw new DomainException("An account with this email already exists.");

        var tenant = new Tenant
        {
            Name = request.BusinessName.Trim(),
            CashierCap = 5
        };
        await _tenantRepository.AddAsync(tenant, ct);

        var admin = new User
        {
            Name = request.AdminName.Trim(),
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = "Admin",
            IsActive = true,
            TenantId = tenant.Id
        };
        await _userRepository.AddAsync(admin, ct);

        await _unitOfWork.SaveChangesAsync(ct);

        var token = _jwtService.GenerateToken(admin);
        return new RegisterResult(token, admin.Name, admin.Email, admin.Role);
    }
}
```

> `Tenant.Id` is assigned at construction (`BaseEntity.Id = Guid.NewGuid()`), so `admin.TenantId = tenant.Id` is valid before save. Both inserts are flushed in one `SaveChangesAsync` (single transaction). `Tenant` is not `ITenantScoped` and `User` is not `ITenantScoped`, so the auto-stamp guard in Task 5 does not fire here.

- [ ] **Step 6: Add the anonymous endpoint**

Replace the body of `backend/src/POS.API/Controllers/AuthController.cs` to add the register action:

```csharp
using MediatR;
using Microsoft.AspNetCore.Mvc;
using POS.Application.Auth.Commands.Login;
using POS.Application.Auth.Commands.Register;

namespace POS.API.Controller;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    public AuthController(IMediator mediator) => _mediator = mediator;

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }
}
```

(The controller has no `[Authorize]`, so `register` is anonymous.)

- [ ] **Step 7: Write the failing registration tests**

`backend/tests/POS.Infrastructure.Tests/RegistrationTests.cs`:

```csharp
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Application.Auth.Commands.Register;
using POS.Application.Common.Interfaces;
using POS.Domain.Entities;
using POS.Domain.Exceptions;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;
using POS.Infrastructure.Services;
using POS.Infrastructure.Tests.Fakes;
using Xunit;

namespace POS.Infrastructure.Tests;

public class RegistrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _ctx;
    private readonly RegisterCommandHandler _handler;

    private sealed class StubJwtService : IJwtService
    {
        public string GenerateToken(User user) => $"token-for-{user.Email}";
    }

    public RegistrationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _ctx = new AppDbContext(options, new FakeCurrentUser { TenantId = null });
        _ctx.Database.EnsureCreated();

        var uow = new UnitOfWork(_ctx);
        _handler = new RegisterCommandHandler(
            new TenantRepository(_ctx),
            new UserRepository(_ctx),
            uow,
            new StubJwtService(),
            new PasswordHasher());
    }

    [Fact]
    public async Task Register_creates_tenant_and_admin_and_returns_token()
    {
        var result = await _handler.Handle(
            new RegisterCommand("Aling Nena Store", "Nena Cruz", "nena@store.ph", "password123"),
            CancellationToken.None);

        Assert.Equal("Admin", result.Role);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));

        var tenant = Assert.Single(await _ctx.Tenants.ToListAsync());
        Assert.Equal("Aling Nena Store", tenant.Name);
        Assert.Equal(5, tenant.CashierCap);

        var user = await _ctx.Users.SingleAsync();
        Assert.Equal("Admin", user.Role);
        Assert.Equal(tenant.Id, user.TenantId);
        Assert.Equal("nena@store.ph", user.Email);
    }

    [Fact]
    public async Task Register_rejects_duplicate_email()
    {
        await _handler.Handle(
            new RegisterCommand("Store One", "Owner One", "dup@store.ph", "password123"),
            CancellationToken.None);

        await Assert.ThrowsAsync<DomainException>(() =>
            _handler.Handle(
                new RegisterCommand("Store Two", "Owner Two", "DUP@store.ph", "password123"),
                CancellationToken.None));
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }
}
```

- [ ] **Step 8: Run the tests**

Run: `dotnet test backend/tests/POS.Infrastructure.Tests`
Expected: PASS — both registration tests green, plus the 4 isolation tests from Task 5.

- [ ] **Step 9: Build the whole solution**

Run: `dotnet build`
Expected: Build succeeded, 0 errors.

- [ ] **Step 10: Commit**

```bash
git add backend/src/POS.Domain/Interfaces/ITenantRepository.cs backend/src/POS.Infrastructure/Persistence/Repositories/TenantRepository.cs backend/src/POS.Infrastructure/DependencyInjection.cs backend/src/POS.Application/Auth/Commands/Register backend/src/POS.API/Controllers/AuthController.cs backend/tests/POS.Infrastructure.Tests/RegistrationTests.cs
git commit -m "feat: public self-serve registration (tenant + admin, atomic)"
```

---

## Task 9: Frontend registration flow

**Files:**
- Modify: `frontend/src/types/index.ts`
- Modify: `frontend/src/features/auth/services/authService.ts`
- Modify: `frontend/src/features/auth/hooks/useAuth.ts`
- Create: `frontend/src/features/auth/screens/RegisterScreen.tsx`
- Modify: `frontend/src/features/auth/screens/LoginScreen.tsx`
- Modify: `frontend/src/App.tsx`

**Interfaces:**
- Consumes: `POST /auth/register` returning `LoginResult` (`{ token, name, email, role }`); the existing `useAuthStore.setAuth(token, user)`.
- Produces: `RegisterPayload` type; `authService.register(payload)`; `useAuth().register(payload)`; `/register` route.

- [ ] **Step 1: Add the `RegisterPayload` type**

In `frontend/src/types/index.ts`, after the `LoginResult` interface, add:

```typescript
export interface RegisterPayload {
  businessName: string;
  adminName: string;
  email: string;
  password: string;
}
```

- [ ] **Step 2: Add `register` to the auth service**

Replace the body of `frontend/src/features/auth/services/authService.ts`:

```typescript
import api from '../../../services/api';
import type { LoginResult, RegisterPayload } from '../../../types';

export const authService = {
  login: async (email: string, password: string): Promise<LoginResult> => {
    const { data } = await api.post<LoginResult>('/auth/login', { email, password });
    return data;
  },

  register: async (payload: RegisterPayload): Promise<LoginResult> => {
    const { data } = await api.post<LoginResult>('/auth/register', payload);
    return data;
  },
};
```

- [ ] **Step 3: Add `register` to the `useAuth` hook**

In `frontend/src/features/auth/hooks/useAuth.ts`, add a `register` function mirroring `login` and include it in the returned object. Add the import:

```typescript
import type { RegisterPayload } from '../../../types';
```

Add inside the hook, after the `login` function:

```typescript
  const register = async (payload: RegisterPayload) => {
    setLoading(true);
    setError(null);
    try {
      const result = await authService.register(payload);
      setAuth(result.token, {
        name: result.name,
        email: result.email,
        role: result.role,
      });
    } catch (err) {
      setError(getApiErrorMessage(err, 'Registration failed.'));
    } finally {
      setLoading(false);
    }
  };
```

Change the return statement to include `register`:

```typescript
  return { login, register, logout, user, token, loading, error };
```

- [ ] **Step 4: Create the `RegisterScreen`**

`frontend/src/features/auth/screens/RegisterScreen.tsx` (reuses the existing `.login-*` / `.field` / `.input` / `.btn` classes):

```tsx
import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';

export const RegisterScreen = () => {
  const { register, loading, error, token } = useAuth();
  const navigate = useNavigate();
  const [businessName, setBusinessName] = useState('');
  const [adminName, setAdminName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');

  useEffect(() => {
    if (token) navigate('/items', { replace: true });
  }, [token, navigate]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await register({ businessName, adminName, email, password });
  };

  return (
    <div className="login-wrap">
      <form className="login-card" onSubmit={handleSubmit}>
        <div className="login-brand">
          <div className="brand-mark">T</div>
          <div>
            <div className="brand-name">Tindahan</div>
            <div className="brand-sub">POS &amp; Inventory</div>
          </div>
        </div>

        <h1 className="login-title">Create your store</h1>
        <p className="login-lead">
          Set up your business account in a few seconds.
        </p>

        {error && (
          <div className="login-error" role="alert">
            <span aria-hidden="true">⚠</span>
            {error}
          </div>
        )}

        <div className="field">
          <label htmlFor="businessName">Business name</label>
          <input
            id="businessName"
            className="input"
            type="text"
            placeholder="Aling Nena Store"
            value={businessName}
            onChange={(e) => setBusinessName(e.target.value)}
            required
          />
        </div>

        <div className="field">
          <label htmlFor="adminName">Your name</label>
          <input
            id="adminName"
            className="input"
            type="text"
            placeholder="Nena Cruz"
            value={adminName}
            onChange={(e) => setAdminName(e.target.value)}
            required
          />
        </div>

        <div className="field">
          <label htmlFor="email">Email</label>
          <input
            id="email"
            className="input"
            type="email"
            autoComplete="username"
            placeholder="you@store.ph"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
          />
        </div>

        <div className="field">
          <label htmlFor="password">Password</label>
          <input
            id="password"
            className="input"
            type="password"
            autoComplete="new-password"
            placeholder="At least 8 characters"
            minLength={8}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
          />
        </div>

        <button
          type="submit"
          className="btn btn-primary btn-block"
          disabled={loading}
        >
          {loading ? <span className="spinner" aria-hidden="true" /> : null}
          {loading ? 'Creating…' : 'Create account'}
        </button>

        <p className="login-lead" style={{ textAlign: 'center', marginTop: '1rem' }}>
          Already have an account? <Link to="/login">Sign in</Link>
        </p>
      </form>
    </div>
  );
};
```

- [ ] **Step 5: Add a "Create an account" link to the login screen**

In `frontend/src/features/auth/screens/LoginScreen.tsx`, change the import line to also import `Link`:

```tsx
import { useNavigate, Link } from 'react-router-dom';
```

Add a link below the submit button, just before the closing `</form>`:

```tsx
        <p className="login-lead" style={{ textAlign: 'center', marginTop: '1rem' }}>
          New here? <Link to="/register">Create an account</Link>
        </p>
```

- [ ] **Step 6: Add the `/register` route**

In `frontend/src/App.tsx`, add the import:

```tsx
import { RegisterScreen } from './features/auth/screens/RegisterScreen';
```

Add the route next to the login route (inside `<Routes>`, outside `ProtectedRoute`):

```tsx
        <Route path="/register" element={<RegisterScreen />} />
```

- [ ] **Step 7: Build and lint the frontend**

Run from `frontend/`:

```bash
npm run build
npm run lint
```

Expected: both succeed, 0 errors.

- [ ] **Step 8: Commit**

```bash
git add frontend/src/types/index.ts frontend/src/features/auth/services/authService.ts frontend/src/features/auth/hooks/useAuth.ts frontend/src/features/auth/screens/RegisterScreen.tsx frontend/src/features/auth/screens/LoginScreen.tsx frontend/src/App.tsx
git commit -m "feat: self-serve registration screen and route"
```

---

## Done-criterion verification (manual, end of Slice A)

After all tasks, prove cross-tenant isolation end-to-end:

- [ ] **Step 1:** Start the API (`dotnet run --project backend/src/POS.API`) and frontend (`npm run dev`).
- [ ] **Step 2:** Register business **A** (e.g. `a@store.ph`). Create one item under A.
- [ ] **Step 3:** Log out, register business **B** (e.g. `b@store.ph`).
- [ ] **Step 4:** Confirm B's item list, stock levels, and sales are **empty** — none of A's data is visible.
- [ ] **Step 5:** Create an item under B, log back in as A, confirm A sees only A's item (not B's).
- [ ] **Step 6:** Confirm the `SuperAdmin` account (CLI) can log in but sees no tenant data through normal screens (default-deny).

Slice A is complete when steps 1–6 hold. **Slice B (cashier user-module) is planned separately.**
