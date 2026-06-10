using TaskManager.Application.Abstractions;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.Infrastructure.Persistence;

/// <summary>Seeds a demo user and sample tasks for demonstration purposes (idempotent).</summary>
public sealed class DataSeeder
{
    public const string DemoEmail = "demo@taskmanager.local";
    public const string DemoPassword = "Demo123!";

    private readonly IUserRepository _users;
    private readonly ITaskRepository _tasks;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IClock _clock;

    public DataSeeder(IUserRepository users, ITaskRepository tasks, IPasswordHasher passwordHasher, IClock clock)
    {
        _users = users;
        _tasks = tasks;
        _passwordHasher = passwordHasher;
        _clock = clock;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await _users.ExistsByEmailAsync(DemoEmail, ct))
            return; // Already seeded.

        var now = _clock.UtcNow;
        var demo = User.Create(DemoEmail, _passwordHasher.Hash(DemoPassword), "Demo User", now);
        await _users.AddAsync(demo, ct);

        var samples = new[]
        {
            TaskItem.Create(demo.Id, "Welcome to Task Manager",
                "This is a seeded sample task. Edit or delete it to get started.", now.AddDays(1), now,
                TaskItemStatus.Todo),
            TaskItem.Create(demo.Id, "Review the architecture",
                "Walk through Domain, Application, Infrastructure and API layers.", now.AddDays(2), now,
                TaskItemStatus.InProgress),
            TaskItem.Create(demo.Id, "Ship the demo",
                "Mark tasks as done once completed.", now.AddDays(-1), now,
                TaskItemStatus.Done)
        };

        foreach (var task in samples)
            await _tasks.AddAsync(task, ct);
    }
}
