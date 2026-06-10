# Presentation — Thought Process & Design Decisions

A walkthrough of *why* the project looks the way it does, intended to support the live
presentation and code review.

---

## 1. Reading the brief carefully

Two requirements drive almost every decision:

1. **"No Entity Framework, Dapper, or Mediator."** This is the heart of the exercise — it's a test
   of whether I can build a clean data-access layer and request flow *by hand*. So the
   interesting engineering is the **raw ADO.NET repository** and the decision to have controllers
   call **services directly** rather than reach for MediatR.
2. **Clean Architecture + TDD.** These dictate the project layout and the order of work.

I also unified the main application's domain with the **GenAI task-management** exercise, so the
whole submission tells one story instead of two unrelated ones.

## 2. Architecture

```
Api  ─┐            ┌─►  Application  ──►  Domain
      ├─► (DI) ─────┤        ▲
Web ──┘            └─►  Infrastructure ──┘   (implements Application's interfaces)
```

- **Domain** — pure C#: `User`, `TaskItem`, `TaskItemStatus`, guarded factory methods, a
  `DomainException`. No dependencies, so the rules are trivially unit-testable.
- **Application** — the business logic: `TaskService` and `AuthService`, plus the **interfaces**
  the outside world must satisfy (`ITaskRepository`, `IUserRepository`, `IPasswordHasher`,
  `IJwtTokenGenerator`, `IClock`). It depends on **nothing** framework-related. This is what
  "independent of the data layer and the API" means in practice — I can swap SQLite for anything
  by implementing the interfaces.
- **Infrastructure** — the concrete adapters: SQLite ADO.NET repositories, BCrypt, JWT, a
  `SystemClock`, the schema initializer and the seeder.
- **Api** — the composition root: wires Infrastructure into Application's interfaces, configures
  JWT bearer auth, Swagger, CORS, and a global exception → ProblemDetails middleware.
- **Web** — a Blazor WebAssembly SPA that is just *another client* of the API.

**Talking point:** the dependency arrows only ever point inward. The Domain doesn't know the
Application exists; the Application doesn't know SQLite or ASP.NET exist.

## 3. Data access without an ORM

`Microsoft.Data.Sqlite` with parameterized commands and a tiny `DbValue` helper for
CLR ↔ TEXT conversions (Guids as strings, dates as ISO-8601 round-trip, status as int). Entities
are rehydrated through a `FromPersistence(...)` factory so persistence doesn't have to re-run
creation guards, while new entities go through `Create(...)` which does.

**Talking points:** SQL injection is prevented by parameters everywhere; the in-memory SQLite test
strategy (shared cache + keep-alive connection) means the repository tests run the *real* SQL.

## 4. Authentication & authorization

- Register/login issue a **JWT** (HS256). Passwords are only ever stored as **BCrypt** hashes.
- The Tasks API is `[Authorize]` at the controller level; the user id comes from the token claims.
- **Security decision worth highlighting:** accessing another user's task returns **404, not 403**,
  so the API never reveals that a resource exists. There's a test for exactly this.
- The Auth API intentionally exposes both an **authorized** endpoint (`/me`) and an **anonymous**
  one (`/ping`) to satisfy the "authorized and non-authorized endpoints" requirement explicitly.

## 5. TDD

The Domain and Application layers were written test-first (red → green → refactor); Infrastructure
and API followed immediately with tests against real SQLite and the real HTTP pipeline. **53 tests**
total. The point of testing the data layer against an actual database (rather than mocking it) is
that the hand-written SQL is the riskiest code in the solution — mocks would prove nothing there.

## 6. Frontend

Blazor WebAssembly, chosen because it's a genuine SPA that talks to the API **over HTTP** and holds
the JWT client-side — the same integration shape as React/Vue, which is what the brief describes.

- A custom `AuthenticationStateProvider` reads the JWT from `localStorage`, parses its claims, and
  honors expiry; a `DelegatingHandler` attaches the bearer token to every API call.
- Typed clients (`AuthApiClient`, `TaskApiClient`) keep components thin; API errors become a
  friendly `ApiException` surfaced inline in the UI.
- Responsive Bootstrap layout, route-level authorization (unauthenticated users are redirected to
  login), loading/error/empty states, and inline validation.

## 7. Trade-offs & what I'd do next

- **"Two APIs"** is implemented as two clearly separated controllers (Tasks + Auth) in one host.
  If the panel prefers a literal two-host split, it's a small refactor — the layering already
  supports it.
- **SQLite** is ideal for a zero-setup demo; for production I'd implement the same repository
  interfaces against a server database and add real migrations.
- **Token storage** in `localStorage` is pragmatic for a demo; a hardened deployment would prefer
  short-lived tokens + refresh, or cookie-based auth, to reduce XSS exposure.
- Next steps I'd add with more time: refresh tokens, paging/filtering/sorting on the task list,
  optimistic concurrency on update, and a small set of bUnit component tests for the frontend.

## 8. Suggested demo order (5 minutes)

1. Show the **solution structure** and the dependency direction (1 min).
2. Open `SqliteTaskRepository` — the hand-written SQL — and `TaskService` — the ownership rule
   (1 min).
3. Run `dotnet test` — all green (30s).
4. Run the API + Blazor app; **Use demo** → create / edit / delete a task (1.5 min).
5. Show cross-user isolation and a `401`/`400` in Swagger (1 min).
6. Walk through `docs/GENAI.md` — the prompt and the real bugs caught (1 min).
