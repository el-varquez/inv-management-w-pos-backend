# POS & Inventory Management — Backend

A single-store, on-premise POS and inventory management API for small Philippine retailers — sari-sari stores, coffee shops, and similar one-location shops.

Built with **C# / ASP.NET Core (.NET 9)**, **PostgreSQL**, and **JWT auth**, following Clean Architecture with CQRS.

> This is the API. It serves two client heads:
> - **Web admin** — React + TypeScript, in [inv-management-w-pos-frontend](https://github.com/el-varquez/inv-management-w-pos-frontend)
> - **Register** — WPF desktop, in [inv-management-w-pos-register](https://github.com/el-varquez/inv-management-w-pos-register)
>
> Both talk to this API over HTTP. The register holds no reference to any backend assembly.

## Why it exists

Most small PH retailers run on notebooks and calculators, and off-the-shelf POS software assumes a purchasing department they don't have. This one is deliberately scoped to how those shops actually operate:

- **No purchase-order workflow.** Restocking is one **Receive Stock** action (quantity, unit cost, optional supplier and notes) that records the cost as an expense automatically. Reports derive expenses from stock movements — there is no separate expense-entry module by design.
- **Acknowledgment Receipts**, not Official Receipts.
- **Payment methods are admin-managed data**, seeded as Cash / GCash / Maya and recorded manually.
- **Utang (store credit) is first-class**, because these shops run on it — but it is kept strictly out of every sales figure.

### One store, one PC

There is no tenancy, no subscription billing, and no platform console. The system runs on a single Windows machine: this API plus PostgreSQL, with the web admin and the register as its two front ends.

## The line between sales and invoices

This is the most important rule in the domain, and it shapes most of the code:

> **The register moves inventory. The web admin moves utang money.**

- A **`Sale`** is always a paid sale. It takes a payment method, feeds every money figure, and lives in a shift.
- An **`Invoice`** is a utang charge. It deducts stock, charges the suki's ledger in full, and **touches no drawer** — it writes no `Sale`, no `Payment`, and no cash movement. No shift or day read carries a utang figure.
- A **`Payment`** is a collection against a suki's balance. It is web-only: no payment method, no shift, no drawer movement, and no open-shift gate.

Balance is `Σ non-voided invoices − Σ non-voided payments`, computed and never stored. A negative balance is legal — it is store credit.

`StoreSettings.AcceptUtang` (default **false**) gates the entire feature through `Common/UtangGate`. Reads stay open; every utang write is refused while it is off. Turning it off while sukis still owe money requires admin credentials in the request body.

## Features

| Module | Capability |
|---|---|
| **Auth** | JWT bearer auth, BCrypt hashing, two roles (`Admin` / `Cashier`), set-password-at-login bootstrap |
| **Items** | Catalog with categories, item codes, barcodes, search, server-side pagination |
| **Categories** | Two seeded system categories — `Inventory Item` and `Service`. The category derives whether an item tracks stock |
| **Inventory** | Receive stock (with create-on-import), adjust, stocktake, low-stock alerts, movement history, valuation |
| **Composite items** | Recipes and bundles with true consumption-on-sale — selling a composite decrements each component, refunds restore it |
| **Sales** | Sale creation, history, refunds with cost snapshots for accurate historical margins |
| **Invoices** | Utang charges — inventory-only, numbered `INV-yyyyMMdd-0001`, Admin-voidable |
| **Utang** | Suki directory, per-suki ledger, collections, payment void/edit, overdue reminders |
| **Shifts & Days** | Starting cash, drawer movements, X read (ends a shift), Z read (ends the business day) |
| **Reports** | Sales, Expenses, Profit, and Best Sellers, with category and item filters |
| **Dashboard** | Today's takings, stock health, payment split, utang outstanding, sales trend |
| **Payment methods** | Admin-managed CRUD; deactivate rather than delete, and Cash can never be turned off |
| **Cashiers** | Admin-managed cashier accounts; the password is optional at creation |
| **Settings** | Single-row store settings — name, address, receipt footer, utang markup and reminder days |

### Days and shifts

`BusinessDay` aggregates `Shift`s, both sequentially numbered. Opening a shift auto-opens a day when none is open.

- The **X read** ends a shift (cashier handover). It freezes a per-shift snapshot including refunds and per-method sales rows, with method names frozen at close so a later rename never rewrites history.
- The **Z read** ends the business day. It is refused while a shift is open, sums the member shifts' X reads, and freezes. A later correction to a member shift never rewrites a closed day.

Expected cash is **pooled across every payment method**:

```
expected cash = starting cash + net sales (all methods) + drawer movements net
```

The store counts one figure covering the drawer and any e-wallet balance together. No utang value enters this formula.

After a Z read, no new shift may open until the next calendar day — the guard keys on the day's *opening* date, so a late close never locks out the next morning. An Admin can undo a Z read the same day via `POST /api/days/{id}/reopen`.

### Security

- Sliding-window rate limiting (5 per 5 min per IP) on a single named `login` policy, shared by `/api/auth/*` and every endpoint that takes credentials in its body
- HSTS in non-development environments, with forwarded-header handling for proxied deployments
- BCrypt hashing, parameterized queries throughout (EF Core), generic authentication errors

## Architecture

Clean Architecture with a strict dependency direction:

```
POS.Domain  ←  POS.Application  ←  POS.Infrastructure  ←  POS.API
```

| Project | Responsibility |
|---|---|
| **POS.Domain** | Entities, enums, domain events, repository interfaces, domain exceptions. No outward dependencies. |
| **POS.Application** | CQRS via **MediatR** — commands and queries per module, each with **FluentValidation** validators run through a `ValidationBehaviour` pipeline. |
| **POS.Infrastructure** | `AppDbContext` (Npgsql), repository implementations, Unit of Work, JWT service, BCrypt hasher, EF Core migrations, startup seeders. |
| **POS.API** | Thin controllers dispatching to MediatR, exception-handling middleware, JWT bearer auth with role policies. |

Each feature is a vertical slice under `src/POS.Application/<Module>/{Commands,Queries,EventHandlers}/<Action>/`. A new command only needs a validator class beside it — the pipeline picks it up with no wiring.

### Domain events and the double-save

`SaveChangesAsync` saves, publishes any collected domain events through MediatR, then saves **again** so handler-made changes persist in the same call. Stock movement rides on this: `SaleCompleted`, `SaleRefunded`, `InvoiceCreated`, and `InvoiceVoided` are thin calls into `POS.Application/Common/StockLedger.cs`, which owns deduction and restoration. Composite buildable stock is computed by `Common/CompositeStock.cs`.

### First-run seeding

A fresh database bootstraps itself on startup — there is no seeding CLI and no registration endpoint:

| Seeder | Effect |
|---|---|
| `AdminSeeder` | When `Users` is empty, creates one `admin` account with a **null password hash** |
| `CategorySeeder` | Ensures the `Inventory Item` and `Service` system categories |
| `PaymentMethodSeeder` | Ensures Cash / GCash / Maya with fixed GUIDs, idempotent by id or name |

A null hash means "password not set". The first login returns `passwordSetupRequired: true` with the user's name and role but no token, and the client routes to a set-password screen — admins in the web admin, cashiers at the register. `setup-password` is server-gated and refused once a hash exists.

**Tech:** .NET 9 · PostgreSQL · EF Core 9 · MediatR · FluentValidation · BCrypt.Net · Swashbuckle

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
dotnet run --project src/POS.API
```

The API listens on `http://localhost:5103`. Swagger UI is available in development. **Pending migrations run automatically on startup** — `dotnet ef database update` is only needed if you want to apply them without booting the API.

On first run, log in as `admin` with an empty password and set one when prompted.

### Configuration

`.env` is gitignored and loaded at startup via DotNetEnv. Keys use the ASP.NET Core `__` nesting convention:

| Key | Notes |
|---|---|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string |
| `Jwt__Key` | HMAC-SHA256 signing key — **minimum 32 characters** |
| `Jwt__Issuer` / `Jwt__Audience` | Token issuer and audience |
| `Cors__AllowedOrigins` | Comma-separated browser origins. Defaults to `http://localhost:5173` |

> **`.env` overrides the process environment.** `Env.Load()` clobbers existing variables, so prefixing a command with `ConnectionStrings__DefaultConnection=…` does *not* retarget the database — it will still use whatever `.env` names. To point at another database, edit `.env` or pass a command-line argument, which outranks both:
>
> ```bash
> dotnet run --project src/POS.API -- --urls "http://localhost:5199" \
>   --ConnectionStrings:DefaultConnection "Host=localhost;Port=5432;Database=pos_scratch;Username=postgres;Password=…"
> ```

### Migrations

Both flags are required:

```bash
dotnet ef migrations add <Name> --project src/POS.Infrastructure --startup-project src/POS.API
dotnet ef database update  --project src/POS.Infrastructure --startup-project src/POS.API
```

### Run with Docker

```bash
cp .env.example .env      # POSTGRES_PASSWORD and Jwt__Key are required
docker compose up --build
```

Brings up the API on port `5103` alongside a PostgreSQL 16 container with a healthcheck and a persistent volume.

### Tests

```bash
dotnet test
dotnet test --filter "FullyQualifiedName~InvoiceModuleTests"   # one class
```

**302 xUnit facts.** Handlers run against a real `AppDbContext` on EF Core's SQLite in-memory provider, exercising the full pipeline including domain-event dispatch — there are no repository mocks. SQLite never runs migrations, so any test class touching sales seeds `PaymentMethodSeeder.Seed(...)` right after `EnsureCreated`.

## CI

Three single-concern GitHub Actions workflows, each with a `checks` job:

| Workflow | What it runs |
|---|---|
| **Build & Test CI** | restore, Release build, `dotnet test` (no DB service needed) |
| **Architecture CI** | `node scripts/check-architecture.mjs` — csproj reference names locked by `architecture/dependencies.json`, composition-root containment, slice grammar |
| **Conventions CI** (PR only) | `node scripts/check-conventions.mjs` — Conventional Commits v1.0.0 and `<type>/<kebab>` branch names |

Run them locally with `node scripts/<name>.mjs`. The repo has no `package.json` on purpose. A deliberate dependency change must update `architecture/dependencies.json` in the same PR.

## API surface

All routes are prefixed with `/api` (67 endpoints).

| Controller | Route | Access |
|---|---|---|
| `AuthController` | `/auth` | Anonymous — rate limited |
| `ItemsController` | `/items` | Authenticated; create/update/delete `Admin` |
| `CategoriesController` | `/categories` | Authenticated; writes `Admin` |
| `InventoryController` | `/inventory` | `Admin` |
| `SalesController` | `/sales` | Authenticated; refund `Admin` |
| `InvoicesController` | `/invoices` | Authenticated; void `Admin` |
| `UtangController` | `/utang` | Authenticated; payment void/edit `Admin` |
| `ShiftsController` | `/shifts` | Authenticated; corrections `Admin` |
| `DaysController` | `/days` | Authenticated; close and reopen `Admin` |
| `ReportsController` | `/reports` | `Admin` |
| `DashboardController` | `/dashboard` | `Admin` |
| `PaymentMethodsController` | `/payment-methods` | Read authenticated; writes `Admin` |
| `CashiersController` | `/cashiers` | `Admin` |
| `ProfileController` | `/profile` | Authenticated — self-scoped, no IDs in URLs |
| `SettingsController` | `/settings` | Read authenticated; writes `Admin`. `store-name` is anonymous |

### Error contract

Every error returns a consistent body, which both clients depend on:

```json
{ "error": "No open shift — declare starting cash to start selling." }
```

`ExceptionHandlingMiddleware` maps `NotFoundException` → 404, `DomainException` → 400, FluentValidation `ValidationException` → 400 (messages joined), and anything else → 500 with a generic message. Handlers signal failure by throwing these domain exceptions, never by returning error DTOs.

`GET /api/payment-methods` returns inactive methods too — every client filters for itself. `requires-reference` is enforced client-side; the server stays lenient.

## License

Not currently licensed for reuse.
