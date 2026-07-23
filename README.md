# POS & Inventory Management — Backend

A multi-tenant POS and inventory management API for small Philippine businesses — sari-sari stores, coffee shops, and similar single-location retail.

Built with **C# / ASP.NET Core (.NET 9)**, **PostgreSQL**, and **JWT auth**, following Clean Architecture with CQRS.

> This is the API. The React web admin lives in [inv-management-w-pos-frontend](https://github.com/el-varquez/inv-management-w-pos-frontend).

## Why it exists

Most small PH retailers run on notebooks and calculators, and off-the-shelf POS software assumes a purchasing department they don't have. This one is deliberately scoped to how those shops actually operate:

- **No purchase-order workflow.** Restocking is one **Add Stock** action (quantity, unit cost, optional supplier, date) that records the cost as an expense automatically. Reports derive expenses from stock movements — there is no separate expense-entry module by design.
- **Acknowledgment Receipts**, not Official Receipts.
- **Cash, GCash, and Maya** as payment types, recorded manually.

## Features

| Module | Capability |
|---|---|
| **Auth** | JWT bearer auth, BCrypt password hashing, three roles (`SuperAdmin` / `Admin` / `Cashier`), self-serve tenant registration |
| **Items** | Catalog with categories, searchable listings, server-side pagination |
| **Inventory** | Add/adjust stock, stocktake, low-stock alerts, movement history, stock valuation |
| **Composite items** | Recipes and bundles with true consumption-on-sale — selling a composite decrements each component's stock, and refunds restore it |
| **Sales** | POS register, sales history, refunds with cost snapshots for accurate historical margins |
| **Reports** | Sales, Expenses, Profit, and Best Sellers, with category and item filters |
| **Cashiers** | Admin-managed cashier accounts with a per-tenant seat cap |
| **Platform** | `SuperAdmin` console — tenant onboarding, suspend/reactivate, per-tenant user management |

### Multi-tenancy

Tenant isolation is enforced at the data layer, not in controllers. An EF Core **global query filter** scopes every business entity to the caller's `tenant_id` (carried as a JWT claim) and stamps it automatically on insert. `Tenant` and `User` are deliberately left unfiltered so login can resolve globally. `SuperAdmin` carries a null `TenantId`, so it sees nothing through normal repositories — default-deny rather than default-allow.

### Security

- Sliding-window rate limiting on `/api/auth/*`, IP-partitioned, returning `429` with `Retry-After`
- HSTS in non-development environments, with forwarded-header handling for proxied deployments
- BCrypt hashing, parameterized queries throughout (EF Core), generic authentication errors

## Architecture

Clean Architecture with a strict dependency direction:

```
POS.Domain  ←  POS.Application  ←  POS.Infrastructure  ←  POS.API
                      ↑______________________________________|
```

| Project | Responsibility |
|---|---|
| **POS.Domain** | Entities, enums, domain events, repository interfaces, domain exceptions. No outward dependencies. |
| **POS.Application** | CQRS via **MediatR** — commands and queries per module, each with **FluentValidation** validators run through a `ValidationBehaviour` pipeline. |
| **POS.Infrastructure** | `AppDbContext` (Npgsql), repository implementations, Unit of Work, JWT service, BCrypt hasher, EF Core migrations. |
| **POS.API** | Thin controllers dispatching to MediatR, exception-handling middleware, JWT bearer auth with role policies. |

Domain events drive side effects — `SaleCompletedEventHandler` expands composite items and decrements component stock; `SaleRefundedEventHandler` reverses it.

**Tech:** .NET 9 · PostgreSQL · EF Core 9 · MediatR · FluentValidation · AutoMapper · BCrypt.Net · Swashbuckle

## Getting started

### Prerequisites

- .NET 9 SDK
- PostgreSQL 16 (or Docker)

### Run locally

```bash
git clone https://github.com/el-varquez/inv-management-w-pos-backend.git
cd inv-management-w-pos-backend

cp .env.example .env      # then fill in the values below
dotnet build
dotnet ef database update --project src/POS.Infrastructure --startup-project src/POS.API
dotnet run --project src/POS.API
```

The API listens on `http://localhost:5103` (`https://localhost:7038`). Swagger UI is available in development.

### Configuration

`.env` is gitignored and loaded at startup via DotNetEnv. Keys use the ASP.NET Core `__` nesting convention:

| Key | Notes |
|---|---|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string |
| `Jwt__Key` | HMAC-SHA256 signing key — **minimum 32 characters** |
| `Jwt__Issuer` / `Jwt__Audience` | Token issuer and audience |
| `Cors__AllowedOrigins` | Comma-separated browser origins. Defaults to `http://localhost:5173` |

A `DbSeeder` creates a default admin and cashier on first run.

### Run with Docker

```bash
cp .env.example .env      # POSTGRES_PASSWORD and Jwt__Key are required
docker compose up --build
```

Brings up the API on port `5103` alongside a PostgreSQL 16 container with a healthcheck and a persistent volume.

### Tests

```bash
dotnet test
```

xUnit with EF Core's SQLite in-memory provider, covering tenant isolation, registration, the cashier module, the platform console, and composite sales — including COGS calculation and the fractional-quantity guard.

## API surface

All routes are prefixed with `/api`.

| Controller | Route | Access |
|---|---|---|
| `AuthController` | `/auth` | Anonymous — rate limited |
| `ItemsController` | `/items` | Authenticated |
| `CategoriesController` | `/categories` | Authenticated |
| `InventoryController` | `/inventory` | `Admin` |
| `SalesController` | `/sales` | Authenticated |
| `ReportsController` | `/reports` | `Admin` |
| `CashiersController` | `/cashiers` | `Admin` |
| `ProfileController` | `/profile` | `Admin` — self-scoped, no IDs in URLs |
| `PlatformController` | `/platform` | `SuperAdmin` |

Errors return a consistent `{ "error": string }` body, which the frontend depends on.

## Roadmap

- **Done** — foundation, auth, items, inventory, sales, reports, multi-tenancy, cashier module, platform console (tenants + users), tenant profile
- **In progress** — tenant subscriptions and payments (PayMongo for GCash/Maya/cards)
- **Next** — Flutter mobile POS: sales-only, offline-first, queuing pending sales locally and syncing when back online

## License

Not currently licensed for reuse.
