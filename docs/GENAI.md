# Generative AI Portion

> The brief asks me to imagine generating a RESTful API for a task-management system, to show the
> **prompt**, a **representative sample of the output**, and to describe how I **validated**,
> **corrected/improved**, and **handled edge cases, authentication and validation**.
>
> This project was genuinely built with a GenAI coding tool (**Claude Code**, driving the .NET
> CLI). So rather than a hypothetical, this document reflects the **actual** prompt-engineering and
> critical-evaluation workflow used to produce the code in this repository.

---

## 1. The prompt

GenAI output quality is dominated by how much *constraint* and *context* the prompt carries. I
don't ask for "a task API" — I pin the architecture, the hard constraints, the contracts, and the
quality bar. The prompt I used (condensed):

> **Context.** Build a task-management REST API in .NET 9 using **Clean Architecture** and **TDD**.
> Hard constraints from the spec: **no Entity Framework, no Dapper, no Mediator/MediatR.**
>
> **Domain.** `User { Id, Email (unique), PasswordHash, DisplayName, CreatedAtUtc }` and
> `TaskItem { Id, OwnerUserId, Title, Description, Status (Todo|InProgress|Done), DueDateUtc,
> CreatedAtUtc, UpdatedAtUtc }`. A user may only access their own tasks.
>
> **Layers.**
> - *Domain*: entities with guarded factory methods, no external dependencies.
> - *Application*: services (`TaskService`, `AuthService`), repository **interfaces**, DTOs,
>   FluentValidation validators, and app-level exceptions. No framework/data dependencies.
> - *Infrastructure*: **raw ADO.NET** repositories over **SQLite** (`Microsoft.Data.Sqlite`),
>   parameterized SQL, manual mapping; **BCrypt** hashing; **JWT** generation; a seeder.
> - *Api*: controllers calling services directly (no MediatR), JWT bearer auth, an Auth API with
>   **both authorized and anonymous** endpoints, Swagger, and RFC 7807 error responses.
>
> **Endpoints.** CRUD `…/api/tasks` (all authorized, scoped to the caller); `…/api/auth`
> register/login (anonymous), `me` (authorized), `ping` (anonymous).
>
> **Rules & edge cases.** Title required (≤200) / description ≤2000; due date not in the past;
> unique, well-formed email; password ≥8 with a letter and a digit; **cross-user access returns
> 404, not 403** (don't leak existence); `401` for missing/invalid token; validation → `400` with a
> per-field error map.
>
> **Tests first.** xUnit + FluentAssertions + NSubstitute. Repository tests run against a real
> in-memory SQLite DB; API tests use `WebApplicationFactory` for full HTTP round-trips. Write the
> tests before/with each unit and keep the build warning-free.

**Prompt-engineering techniques applied:** lead with the non-negotiable constraints; specify the
data contracts exactly; encode security decisions (404-not-403) *in the prompt* rather than hoping
for them; demand tests in the same breath as code; and set a measurable quality bar
("warning-free", "real in-memory SQLite", "RFC 7807").

---

## 2. Representative output sample

A representative slice — the hand-written ADO.NET repository (the part most affected by the
"no EF/Dapper" constraint). This is the actual generated/refined code in
`src/TaskManager.Infrastructure/Persistence/SqliteTaskRepository.cs`:

```csharp
public async Task<IReadOnlyList<TaskItem>> GetByOwnerAsync(Guid ownerUserId, CancellationToken ct = default)
{
    await using var connection = _factory.Create();
    await connection.OpenAsync(ct);

    await using var cmd = connection.CreateCommand();
    cmd.CommandText = $"SELECT {Columns} FROM Tasks WHERE OwnerUserId = $owner ORDER BY CreatedAtUtc DESC;";
    cmd.Parameters.AddWithValue("$owner", DbValue.FromGuid(ownerUserId));

    var results = new List<TaskItem>();
    await using var reader = await cmd.ExecuteReaderAsync(ct);
    while (await reader.ReadAsync(ct))
        results.Add(Map(reader));

    return results;
}

private static TaskItem Map(SqliteDataReader reader) => TaskItem.FromPersistence(
    DbValue.ReadGuid(reader, 0),
    DbValue.ReadGuid(reader, 1),
    reader.GetString(2),
    DbValue.ReadNullableString(reader, 3),
    (TaskItemStatus)reader.GetInt32(4),
    DbValue.ReadNullableDate(reader, 5),
    DbValue.ReadDate(reader, 6),
    DbValue.ReadDate(reader, 7));
```

And the business-logic ownership check that backs the "404-not-403" decision
(`src/TaskManager.Application/Tasks/TaskService.cs`):

```csharp
private async Task<TaskItem> GetOwnedTaskOrThrowAsync(Guid taskId, Guid ownerUserId, CancellationToken ct)
{
    var task = await _tasks.GetByIdAsync(taskId, ct);

    // Return 404 (not 403) for tasks owned by someone else so existence is not leaked.
    if (task is null || task.OwnerUserId != ownerUserId)
        throw NotFoundException.For("Task", taskId);

    return task;
}
```

---

## 3. How I validated the AI's suggestions

Generated code is a *draft*, not an authority. Validation gates used here:

1. **Compile + warning gate.** `dotnet build` after every layer; the bar was **0 warnings**.
2. **Tests as the contract.** 53 tests across all layers. Crucially, the data and API layers are
   tested against a **real in-memory SQLite database** and the **real HTTP pipeline** — not mocks —
   so the hand-written SQL and the auth/serialization wiring are actually exercised.
3. **Out-of-band smoke test.** I ran the API and hit it with `curl` (login → token → `me` → list →
   create → invalid → wrong-password) to confirm behavior end-to-end, independent of the test code.
4. **Spec cross-check.** Re-read the brief and ticked each requirement (two APIs, authorized/anon
   endpoints, ≥2 non-key fields, no EF/Dapper/Mediator, seeded credentials, …).

## 4. What I corrected or improved (critical thinking)

The most valuable part of working with GenAI is catching what it gets *subtly* wrong. Real defects
found and fixed in this build:

- **JWT validated with the wrong key under test (silent 401s).** Integration tests using a Bearer
  token all returned `401`. Root cause: `Program.cs` read the signing key **eagerly** during
  service registration, *before* `WebApplicationFactory`'s configuration override applied — so the
  bearer validator used one key while the token generator (resolved lazily via `IOptions`) used
  another. Fix: keep the configured key for both sides and swap only the **database** via
  `ConfigureTestServices`. This is exactly the kind of timing bug a green "it compiles" never
  surfaces — only a real round-trip test does.
- **`ProblemDetails` validation errors silently dropped.** The error response was typed via a
  ternary whose common type was the base `ProblemDetails`, so `System.Text.Json` serialized by the
  static type and **sliced off** `ValidationProblemDetails.Errors`. Caught by inspecting the actual
  `curl` response body; fixed by serializing with the runtime type.
- **In-memory SQLite vanishing between calls.** A shared-cache in-memory SQLite database is
  destroyed when its last connection closes. Repository/API tests needed a **keep-alive connection
  opened before** the startup seeder ran, or the schema/seed disappeared. The naive first attempt
  opened it too late; fixed by opening it during host build.
- **Dependency hygiene the AI wouldn't think about.** Pinned **FluentAssertions 6.x** (7+/8+ moved
  to a commercial license — wrong for a public repo); pinned ASP.NET packages to **9.x** (the CLI
  defaulted to .NET 10 builds incompatible with `net9.0`); pinned the **SDK via `global.json`** so a
  classic `.sln` is produced instead of .NET 10's new `.slnx`.
- **Naming collision.** The status enum was renamed `TaskItemStatus` to avoid clashing with
  `System.Threading.Tasks.TaskStatus` (a trap with `ImplicitUsings`).

## 5. How edge cases, authentication and validation were handled

- **Authentication.** JWT bearer; passwords stored only as **BCrypt** hashes; tokens carry the
  user id (`sub` / `NameIdentifier`). The token verify path returns `false` (never throws) on a
  malformed stored hash, so a corrupt value can't accidentally authenticate.
- **Authorization & multi-tenancy.** Every task operation is scoped to the caller's id taken from
  the token; cross-user access returns **404** to avoid leaking existence (verified by a dedicated
  test).
- **Validation.** Two-layer: **Domain** guards enforce structural invariants (e.g. non-empty
  title) even on direct construction, while **FluentValidation** in the Application layer enforces
  request rules (email format, password strength, length limits, due-date-not-in-past). Failures
  surface as `400` with a per-field error map that the Blazor UI renders inline.
- **Edge cases covered by tests.** Missing token (`401`), duplicate email (`409`), wrong password
  (`401`), empty title (`400`), past due date (`400`), non-existent task (`404`), another user's
  task (`404`), null description/due-date round-tripping through SQLite, and timestamp/`UpdatedAt`
  behavior on update.

## 6. Takeaway

GenAI was a strong **accelerator** for boilerplate (DTOs, repository plumbing, test scaffolding)
and a good **first-draft architect**. But the value came from treating its output skeptically:
a strict compile/test/smoke-test gate, reading the actual wire responses, and applying
domain/security/dependency judgment that the model does not reliably supply on its own. The bugs
above were all plausible-looking, compiled cleanly, and would have shipped without that scrutiny.
