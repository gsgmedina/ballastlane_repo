using System.Text.Json.Serialization;

namespace TaskManager.Web.Models;

/// <summary>Client-side mirror of the API task status enum.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TaskItemStatus
{
    Todo = 0,
    InProgress = 1,
    Done = 2
}

public sealed record TaskResponse(
    Guid Id,
    string Title,
    string? Description,
    TaskItemStatus Status,
    DateTime? DueDateUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record CreateTaskRequest(
    string Title,
    string? Description,
    DateTime? DueDateUtc,
    TaskItemStatus? Status);

public sealed record UpdateTaskRequest(
    string Title,
    string? Description,
    DateTime? DueDateUtc,
    TaskItemStatus Status);
