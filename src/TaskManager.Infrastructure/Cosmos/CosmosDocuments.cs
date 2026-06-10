using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.Infrastructure.Cosmos;

/// <summary>
/// Cosmos document for a user. The Cosmos serializer is configured for camelCase, so
/// <see cref="Id"/> is persisted as the required system property "id". Partition key: /id.
/// </summary>
internal sealed class UserDocument
{
    public string Id { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; }

    public static UserDocument FromEntity(User u) => new()
    {
        Id = u.Id.ToString(),
        Email = u.Email,
        PasswordHash = u.PasswordHash,
        DisplayName = u.DisplayName,
        CreatedAtUtc = u.CreatedAtUtc
    };

    public User ToEntity() => User.FromPersistence(
        Guid.Parse(Id), Email, PasswordHash, DisplayName, AsUtc(CreatedAtUtc));

    private static DateTime AsUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
}

/// <summary>
/// Cosmos document for a task. Partition key: /ownerUserId (so a user's tasks live together and
/// list queries stay single-partition).
/// </summary>
internal sealed class TaskDocument
{
    public string Id { get; set; } = default!;
    public string OwnerUserId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public int Status { get; set; }
    public DateTime? DueDateUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public static TaskDocument FromEntity(TaskItem t) => new()
    {
        Id = t.Id.ToString(),
        OwnerUserId = t.OwnerUserId.ToString(),
        Title = t.Title,
        Description = t.Description,
        Status = (int)t.Status,
        DueDateUtc = t.DueDateUtc,
        CreatedAtUtc = t.CreatedAtUtc,
        UpdatedAtUtc = t.UpdatedAtUtc
    };

    public TaskItem ToEntity() => TaskItem.FromPersistence(
        Guid.Parse(Id),
        Guid.Parse(OwnerUserId),
        Title,
        Description,
        (TaskItemStatus)Status,
        DueDateUtc.HasValue ? AsUtc(DueDateUtc.Value) : null,
        AsUtc(CreatedAtUtc),
        AsUtc(UpdatedAtUtc));

    private static DateTime AsUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
