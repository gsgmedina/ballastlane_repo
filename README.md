# Task Manager — BLA .NET Technical Exercise

A small, secure **task management** application built with **.NET 9**, following **Clean
Architecture** and **Test-Driven Development**, with a **hand-written SQLite data-access layer**
(no Entity Framework / Dapper / Mediator) and a **Blazor WebAssembly** frontend that consumes the
API over HTTP with JWT bearer authentication.

> **User story:** *As a busy professional, I want to register and securely sign in, then create,
> view, update and delete my own tasks — each with a title, description, status and due date — so
> that I can track my work in one place and never see anyone else's tasks.*

---

## Why these choices (constraints from the brief)

The exercise explicitly forbids **Entity Framework, Dapper, and Mediator**. Every consequence of
that constraint is deliberate:

| Concern | Decision |
|---|---|
| Data store | **Pluggable**: **SQLite** (`Microsoft.Data.Sqlite`, raw ADO.NET — hand-written parameterized SQL and manual mapping) *or* **Cosmos DB** (native `Microsoft.Azure.Cosmos` SDK). Selectable via config — see [Switching the data store](#switching-the-data-store-sqlite--cosmos-db). Neither uses an ORM. |
| Request dispatch | Controllers call **Application services directly** via constructor injection — no MediatR. |
| Architecture | **Clean Architecture**: `Domain` (no deps) ← `Application` (business logic, interfaces) ← `Infrastructure` (data/security) and `Api` (composition root). |
| Auth | **JWT bearer** tokens, **BCrypt** password hashing. A dedicated Auth API with authorized *and* anonymous endpoints. |
| Validation | **FluentValidation** in the Application layer. |
| Frontend | **Blazor WebAssembly** SPA — a true over-HTTP API client holding the JWT, mirroring how React/Vue integrate. |
| Tests | **xUnit + FluentAssertions + NSubstitute**, integration tests via `WebApplicationFactory`. |

---

## Solution structure

```
TaskManager.sln
├── src/
│   ├── TaskManager.Domain          # Entities, enums, invariants. No dependencies.
│   ├── TaskManager.Application      # Business logic + abstractions (interfaces), DTOs, validators.
│   ├── TaskManager.Infrastructure   # SQLite ADO.NET + Cosmos DB repos, BCrypt, JWT, clock, seeding.
│   ├── TaskManager.Api              # ASP.NET Core Web API (controllers, auth, Swagger).
│   └── TaskManager.Web              # Blazor WebAssembly SPA.
└── tests/
    ├── TaskManager.Domain.Tests
    ├── TaskManager.Application.Tests       # Business rules (mocked repositories)
    ├── TaskManager.Infrastructure.Tests    # Repositories vs. real in-memory SQLite
    └── TaskManager.Api.Tests               # End-to-end HTTP via WebApplicationFactory
```

Dependency direction: `Api → Application → Domain` and `Infrastructure → Application → Domain`.
The Application layer references **no** framework or data technology — it only defines interfaces
that Infrastructure implements, keeping business logic independent of the data layer and the API.

---

## Prerequisites

- **.NET SDK 9.0** (the repo pins it via `global.json`; SDK 8/9/10 may be installed side-by-side).
- A modern browser for the Blazor frontend.

Check with:

```bash
dotnet --version   # should resolve to a 9.0.x SDK
```

---

## Running the application

The backend and frontend run as **two processes**. Open two terminals.

### 1. API

```bash
cd src/TaskManager.Api
dotnet run
```

- HTTPS: `https://localhost:7179` · HTTP: `http://localhost:5195`
- **Swagger UI:** `https://localhost:7179/swagger`
- On first start the API **creates the SQLite schema and seeds** a demo user + sample tasks.

### 2. Frontend (Blazor WebAssembly)

```bash
cd src/TaskManager.Web
dotnet run
```

- App: `https://localhost:7161`
- It calls the API at the URL in `wwwroot/appsettings.json` (`ApiBaseUrl`, default
  `https://localhost:7179`). CORS for the SPA origin is configured in the API's `appsettings.json`.

> If the browser blocks the API's dev certificate, run `dotnet dev-certs https --trust` once,
> or open `https://localhost:7179/swagger` and accept the certificate.

### Seeded demo credentials

| Email | Password |
|---|---|
| `demo@taskmanager.local` | `Demo123!` |

The login screen has a **“Use demo”** button that fills these in.

---

## Running the tests

```bash
dotnet test
```

All four test projects run (Domain, Application, Infrastructure, API — **53 tests**). The
Infrastructure and API suites exercise a **real in-memory SQLite database**, so they cover the
hand-written SQL and the full HTTP pipeline, not just mocks.

---

## Switching the data store (SQLite ⇄ Cosmos DB)

The data store is selectable via configuration — the same repository interfaces are implemented by
two providers, so nothing above the Infrastructure layer changes. **Cosmos DB uses the native
`Microsoft.Azure.Cosmos` SDK (hand-written queries), *not* the EF Core Cosmos provider**, so the
"no Entity Framework" rule still holds.

In `src/TaskManager.Api/appsettings.json`:

```jsonc
"Database": { "Provider": "Sqlite" },   // "Sqlite" (default) or "Cosmos"
```

### Using Cosmos DB (local emulator)

1. Install and start the **Azure Cosmos DB Emulator** (listens on `https://localhost:8081`).
2. Set the provider to `Cosmos` (edit `appsettings.json`, or override at run time):

   ```powershell
   $env:Database__Provider = "Cosmos"
   dotnet run --project src/TaskManager.Api
   ```

3. On startup the app creates the `TaskManager` database and the `Users` (`/id`) and `Tasks`
   (`/ownerUserId`) containers, then seeds the same demo data.

The Cosmos settings (endpoint, key, database/container names) live under the `Cosmos` section in
`appsettings.json` and default to the well-known local-emulator endpoint and key. The emulator's
self-signed certificate is accepted via `Cosmos:BypassCertificateValidation` (true for local dev
only — set it to `false` against a real account).

> **Design notes:** `Users` is partitioned by `/id` (point reads by id; email lookups are
> cross-partition queries, fine at this scale); `Tasks` is partitioned by `/ownerUserId` so a
> user's tasks live together and list queries stay single-partition. Email uniqueness is enforced
> at the application layer (consistent with the SQLite provider). The default SQLite path is
> unaffected and remains the one exercised by the automated tests.

## API reference

### Auth API (`/api/auth`) — the "second API"
| Verb | Route | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/register` | Anonymous | Create a user, returns a JWT |
| POST | `/api/auth/login` | Anonymous | Authenticate, returns a JWT |
| GET  | `/api/auth/me` | **Bearer** | Current user's profile (authorized endpoint) |
| GET  | `/api/auth/ping` | Anonymous | Public endpoint (non-authorized) |

### Tasks API (`/api/tasks`) — all require a Bearer token, scoped to the caller
| Verb | Route | Success | Notes |
|---|---|---|---|
| GET | `/api/tasks` | 200 | The current user's tasks |
| GET | `/api/tasks/{id}` | 200 / 404 | 404 if not found *or* owned by another user |
| POST | `/api/tasks` | 201 | `Location` header to the new task |
| PUT | `/api/tasks/{id}` | 200 / 404 | Full update |
| DELETE | `/api/tasks/{id}` | 204 / 404 | |

Errors are returned as RFC 7807 `application/problem+json`. Validation failures return `400` with
a per-field `errors` map. Missing/invalid token → `401`. Cross-user access → `404` (existence is
never leaked).

---

## Postman collection

A ready-to-import collection lives at
[`postman/TaskManager.postman_collection.json`](postman/TaskManager.postman_collection.json). Import
it into Postman, then run **Auth → Login (demo)** (it stores the JWT automatically) and use the
**Tasks** requests — or **Run collection** to execute the full ordered flow (login → create → get →
update → delete) plus negative tests (401/400). Set the `baseUrl` variable to your API URL
(`https://localhost:7179` or `http://localhost:5195`).

It can also run headless with [newman](https://github.com/postmanlabs/newman):

```bash
npx newman run postman/TaskManager.postman_collection.json --env-var "baseUrl=http://localhost:5195"
```

> If Postman blocks the HTTPS dev certificate, turn off **Settings → SSL certificate verification**,
> or use the HTTP `baseUrl`.

## Further documentation

- [`docs/USER_STORY.md`](docs/USER_STORY.md) — the informal user story driving development.
- [`docs/PRESENTATION.md`](docs/PRESENTATION.md) — thought process, design decisions, trade-offs.
- [`docs/GENAI.md`](docs/GENAI.md) — the Generative AI portion of the assessment.
- [`docs/PLAN.md`](docs/PLAN.md) — the original implementation plan.
