# BLA .NET Technical Interview — Implementation Plan

> Status: **PLAN ONLY** — no code committed yet. This document is the proposal + step-by-step
> plan. Implementation happens locally after approval. Nothing is pushed/committed until the
> repo is finalized and the user explicitly asks.

---

## 1. Summary of the proposal

A **Task Management** application built as a single .NET solution following **Clean Architecture**
and **TDD**, with a hand-written data-access layer (no EF / Dapper / Mediator), a JWT-secured
Web API, and a **Blazor WebAssembly** frontend that consumes that API over HTTP.

The domain is deliberately unified with the GenAI exercise (a task-management REST API), so the
**user story, the built application, and the GenAI write-up all align** and tell one coherent story
during the presentation.

### Confirmed decisions

| Decision | Choice | Rationale |
|---|---|---|
| Data store | **SQLite via `Microsoft.Data.Sqlite` (raw ADO.NET)** | File-based, zero server setup, trivial to seed, and clearly demonstrates a hand-written repository/data-access layer. Complies with the EF/Dapper ban. |
| Frontend | **Blazor WebAssembly (standalone SPA)** | True SPA-over-HTTP integration mirroring how React/Vue consume the API; holds the JWT client-side. Same solution as the backend. |
| Use case | **Task Manager** (tasks: title, description, status, due date, owned by a user) | Cohesive with the mandatory GenAI task-management exercise. |
| Target framework | **.NET 9** (SDKs 8/9/10 are installed) | Modern + broadly available. Fallback to .NET 8 LTS if a reviewer needs maximum compatibility. |

### Hard constraints from the brief (compliance checklist)

- [ ] **No Entity Framework** → raw ADO.NET (`Microsoft.Data.Sqlite`) with manual SQL + mapping.
- [ ] **No Dapper** → no micro-ORM; map `IDataReader` → entities by hand.
- [ ] **No Mediator / MediatR** → controllers call Application services directly via constructor DI.
- [ ] **Clean Architecture** → Domain has zero dependencies; Application depends only on Domain;
      Infrastructure & API depend inward; business logic is independent of data + API.
- [ ] **TDD** → red/green/refactor, tests written first for Domain + Application layers.
- [ ] **Two "APIs"** → a Tasks CRUD API and a separate Auth/Users API (register, login),
      with both **authorized** and **non-authorized** endpoints.
- [ ] **Unit tests for all components** → Domain, Application, Infrastructure, API.
- [ ] **Frontend** → responsive, user-friendly, full CRUD, cleanly structured components/state.
- [ ] **README + seeded demo data/credentials**.
- [ ] **GenAI section** (a MUST) → prompt + output sample + validation/correction/edge-case write-up.
- [ ] **Single public GitHub repo** + presentation (README / text) of the thought process.

---

## 2. Solution architecture

```
TaskManager.sln
│
├── src/
│   ├── TaskManager.Domain          # Enterprise core — entities, enums, domain rules. No deps.
│   ├── TaskManager.Application      # Business logic layer. Depends ONLY on Domain.
│   │                                #   - Service interfaces + implementations (TaskService, AuthService)
│   │                                #   - Abstractions: ITaskRepository, IUserRepository,
│   │                                #     IPasswordHasher, IJwtTokenGenerator, IClock
│   │                                #   - DTOs, validators, app-level exceptions
│   ├── TaskManager.Infrastructure   # Data-access layer. Implements Application abstractions.
│   │                                #   - SQLite raw ADO.NET repositories (hand SQL + mapping)
│   │                                #   - DB initializer + seeder, BCrypt hasher, JWT generator
│   ├── TaskManager.Api              # ASP.NET Core Web API (composition root).
│   │                                #   - TasksController (CRUD, [Authorize])
│   │                                #   - AuthController (register/login + authz/anon endpoints)
│   │                                #   - JWT bearer auth, Swagger, global error handling
│   └── TaskManager.Web              # Blazor WebAssembly SPA. Consumes the API over HTTP.
│
└── tests/
    ├── TaskManager.Domain.Tests
    ├── TaskManager.Application.Tests       # TDD heart — business rules, mocked repositories
    ├── TaskManager.Infrastructure.Tests    # Repositories vs. in-memory SQLite
    └── TaskManager.Api.Tests               # Controller unit tests + WebApplicationFactory integration
```

**Dependency direction (Clean Architecture):**
`Api → Application → Domain` and `Infrastructure → Application → Domain`.
The Domain depends on nothing. Application depends on no framework/data tech — it only
defines interfaces that Infrastructure implements. The API is the composition root that wires
Infrastructure implementations into the Application abstractions via DI.

### Domain model

- **User**: `Id (Guid, PK)`, `Email (unique)`, `PasswordHash`, `DisplayName`, `CreatedAtUtc`.
- **TaskItem**: `Id (Guid, PK)`, `OwnerUserId (FK)`, `Title`, `Description`, `Status` (enum:
  `Todo | InProgress | Done`), `DueDateUtc`, `CreatedAtUtc`, `UpdatedAtUtc`.
  → Satisfies "primary key + at least two other fields" comfortably.

### Business rules (Application layer — independent of data + API)

- Title required, length-bounded; description length-bounded.
- `DueDateUtc` cannot be in the past on creation.
- Status transitions validated (e.g., can't move a `Done` task back arbitrarily — TBD, kept simple).
- A user may only read/update/delete **their own** tasks (ownership enforced server-side).
- Email must be unique and well-formed; password meets a minimum strength policy.

### Tech choices (all permitted — none are EF/Dapper/Mediator)

| Concern | Library |
|---|---|
| Data access | `Microsoft.Data.Sqlite` (raw ADO.NET) |
| Validation | `FluentValidation` (in Application layer) |
| Password hashing | `BCrypt.Net-Next` |
| JWT | `Microsoft.AspNetCore.Authentication.JwtBearer` + `System.IdentityModel.Tokens.Jwt` |
| Tests | `xUnit`, `FluentAssertions`, `NSubstitute` (mocks), `Microsoft.AspNetCore.Mvc.Testing` (integration) |
| API docs | Swagger / Swashbuckle |

---

## 3. API surface

### Auth / Users API (the "second API")
| Verb | Route | Auth | Purpose |
|---|---|---|---|
| POST | `/api/auth/register` | Anonymous | Create a user |
| POST | `/api/auth/login` | Anonymous | Authenticate → returns JWT |
| GET  | `/api/auth/me` | **Authorized** | Current user profile (demonstrates authorized endpoint) |
| GET  | `/api/auth/ping` | Anonymous | Public/non-authorized endpoint demonstration |

### Tasks CRUD API (all `[Authorize]`, scoped to the current user)
| Verb | Route | Returns |
|---|---|---|
| GET | `/api/tasks` | `200` list of the user's tasks |
| GET | `/api/tasks/{id}` | `200` task / `404` |
| POST | `/api/tasks` | `201` + Location header |
| PUT | `/api/tasks/{id}` | `200`/`204` updated / `404` |
| DELETE | `/api/tasks/{id}` | `204` / `404` |

Proper HTTP verbs, status codes, model-validation `400`s, `401` for missing/invalid token,
`403`/`404` for cross-user access (return `404` to avoid leaking existence).

---

## 4. Step-by-step build plan (TDD-driven)

Ordered for the Thu 11th 10:00 (ART) deadline. Each backend step is **test-first**.

### Phase 0 — Scaffolding
1. `git init` a fresh repo (local only; no commits until finalize).
2. Create `TaskManager.sln`; add the 5 `src` projects + 4 `tests` projects with correct refs.
3. Add `.gitignore` (`dotnet new gitignore`), `Directory.Build.props` (nullable, warnings-as-needed),
   solution-wide package versions.

### Phase 1 — Domain
4. Implement `User`, `TaskItem`, `TaskStatus` enum, domain guards/exceptions.
5. `Domain.Tests`: entity invariants (e.g., guard clauses).

### Phase 2 — Application (TDD core)
6. Define abstractions: `ITaskRepository`, `IUserRepository`, `IPasswordHasher`,
   `IJwtTokenGenerator`, `IClock`.
7. **Write tests first** for `TaskService` (CRUD + ownership + validation) and `AuthService`
   (register dup-email, login success/fail, token issuance) using NSubstitute mocks.
8. Implement services + FluentValidation validators + DTOs until green; refactor.

### Phase 3 — Infrastructure (data-access layer)
9. `SqliteConnectionFactory` + `DatabaseInitializer` (CREATE TABLE IF NOT EXISTS, hand SQL).
10. `SqliteTaskRepository`, `SqliteUserRepository` — raw `SqliteCommand`, parameterized SQL,
    manual reader→entity mapping.
11. `BCryptPasswordHasher`, `JwtTokenGenerator`, `DataSeeder` (demo user + sample tasks).
12. `Infrastructure.Tests`: run each repository against a fresh in-memory/temp-file SQLite DB.

### Phase 4 — API
13. `Program.cs`: DI wiring (Infra → Application abstractions), JWT bearer config from
    `appsettings`, Swagger with bearer support, CORS for the Blazor origin, global exception
    middleware → ProblemDetails.
14. `AuthController` + `TasksController` (extract current user id from JWT claims).
15. Run DB init + seed on startup (dev).
16. `Api.Tests`: controller unit tests (mock services) + integration tests via
    `WebApplicationFactory` hitting real endpoints with a real in-memory SQLite DB
    (register → login → CRUD round-trip, plus 401/403/404 cases).

### Phase 5 — Frontend (Blazor WebAssembly)
17. Scaffold `TaskManager.Web` (Blazor WASM standalone).
18. Typed `HttpClient` API clients; `AuthenticationStateProvider` holding the JWT
    (in-memory + `localStorage`), bearer `DelegatingHandler`.
19. Pages: **Login/Register**, **Tasks list** (responsive table/cards), **Create/Edit** form
    (validation), **Delete** confirm. Clean component/state organization; loading + error states.
20. Wire CORS + base address; verify full CRUD round-trip against the API.

### Phase 6 — Docs, seeding, GenAI, polish
21. `README.md`: overview, architecture diagram, **setup/run instructions**, seeded
    **demo credentials**, how to run tests.
22. `docs/USER_STORY.md`: the informal user story driving development.
23. `docs/GENAI.md`: **(mandatory)** the prompt used, representative output, how I validated /
    corrected the AI output, and how edge cases / auth / validation were handled.
24. `docs/PRESENTATION.md`: thought process, design choices, trade-offs, architecture walkthrough.
25. Final review: build clean (no warnings target), all tests green, browser console clean,
    verify seeded demo works end-to-end.

### Phase 7 — Submission (only when user approves)
26. Create a **single public GitHub repo**, push, confirm README renders + repo is public.
27. Draft the email reply: confirm receipt, include the **one** GitHub link + note on the
    presentation doc and GenAI section.

---

## 5. Risks & mitigations

| Risk | Mitigation |
|---|---|
| Hand-written SQL bugs / SQL injection | Parameterized commands everywhere; repository integration tests against real SQLite. |
| Blazor WASM ↔ API CORS / auth friction | Configure CORS early; test the JWT bearer handler with a round-trip before building all pages. |
| "Two APIs" interpretation | Implemented as two clearly separated controllers (Tasks + Auth) with authorized & anonymous endpoints; documented in README. Can be split into two host projects if the panel prefers. |
| In-memory SQLite connection lifetime in tests | Keep a shared open connection per test fixture so the in-memory DB persists for the test. |
| Scope creep vs. deadline | MVP CRUD + auth + tests + 1 clean frontend flow first; polish after green. |

---

## 6. Deliverables recap

1. Public GitHub repo (single link) with the full solution.
2. README with setup + seeded demo credentials.
3. `docs/USER_STORY.md`, `docs/GENAI.md`, `docs/PRESENTATION.md`.
4. Passing test suite across all layers.
5. Email reply confirming receipt + the single repo link.
