using TaskManager.Domain.Common;
using TaskManager.Domain.Enums;

namespace TaskManager.Domain.Entities;

/// <summary>
/// A task owned by a <see cref="User"/>. This is the primary CRUD aggregate of the application.
/// </summary>
public sealed class TaskItem
{
    public const int MaxTitleLength = 200;
    public const int MaxDescriptionLength = 2000;

    public Guid Id { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public TaskItemStatus Status { get; private set; }
    public DateTime? DueDateUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private TaskItem(
        Guid id,
        Guid ownerUserId,
        string title,
        string? description,
        TaskItemStatus status,
        DateTime? dueDateUtc,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        Id = id;
        OwnerUserId = ownerUserId;
        Title = title;
        Description = description;
        Status = status;
        DueDateUtc = dueDateUtc;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    /// <summary>Creates a new task for an owner. Enforces structural invariants.</summary>
    public static TaskItem Create(
        Guid ownerUserId,
        string title,
        string? description,
        DateTime? dueDateUtc,
        DateTime nowUtc,
        TaskItemStatus status = TaskItemStatus.Todo)
    {
        if (ownerUserId == Guid.Empty)
            throw new DomainException("A task must have an owner.");

        var normalizedTitle = NormalizeTitle(title);
        var normalizedDescription = NormalizeDescription(description);

        return new TaskItem(
            Guid.NewGuid(), ownerUserId, normalizedTitle, normalizedDescription, status, dueDateUtc, nowUtc, nowUtc);
    }

    /// <summary>Updates the mutable details of the task and bumps the updated timestamp.</summary>
    public void UpdateDetails(string title, string? description, DateTime? dueDateUtc, DateTime nowUtc)
    {
        Title = NormalizeTitle(title);
        Description = NormalizeDescription(description);
        DueDateUtc = dueDateUtc;
        UpdatedAtUtc = nowUtc;
    }

    /// <summary>Changes the task status and bumps the updated timestamp.</summary>
    public void ChangeStatus(TaskItemStatus status, DateTime nowUtc)
    {
        Status = status;
        UpdatedAtUtc = nowUtc;
    }

    /// <summary>Rehydrates a task from persistence without re-running creation guards.</summary>
    public static TaskItem FromPersistence(
        Guid id,
        Guid ownerUserId,
        string title,
        string? description,
        TaskItemStatus status,
        DateTime? dueDateUtc,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
        => new(id, ownerUserId, title, description, status, dueDateUtc, createdAtUtc, updatedAtUtc);

    private static string NormalizeTitle(string title)
    {
        title = (title ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Title is required.");
        if (title.Length > MaxTitleLength)
            throw new DomainException($"Title must be {MaxTitleLength} characters or fewer.");
        return title;
    }

    private static string? NormalizeDescription(string? description)
    {
        if (description is null)
            return null;
        description = description.Trim();
        if (description.Length == 0)
            return null;
        if (description.Length > MaxDescriptionLength)
            throw new DomainException($"Description must be {MaxDescriptionLength} characters or fewer.");
        return description;
    }
}
