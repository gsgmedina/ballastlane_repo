using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Tasks.Dtos;

/// <summary>Payload to create a task. Status defaults to Todo when omitted.</summary>
public sealed record CreateTaskRequest(
    string Title,
    string? Description,
    DateTime? DueDateUtc,
    TaskItemStatus? Status);

/// <summary>Payload to fully update a task.</summary>
public sealed record UpdateTaskRequest(
    string Title,
    string? Description,
    DateTime? DueDateUtc,
    TaskItemStatus Status);

/// <summary>Task representation returned to clients.</summary>
public sealed record TaskResponse(
    Guid Id,
    string Title,
    string? Description,
    TaskItemStatus Status,
    DateTime? DueDateUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc)
{
    public static TaskResponse FromEntity(TaskItem t) => new(
        t.Id, t.Title, t.Description, t.Status, t.DueDateUtc, t.CreatedAtUtc, t.UpdatedAtUtc);
}
