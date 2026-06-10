namespace TaskManager.Domain.Enums;

/// <summary>
/// Lifecycle status of a task. Named <c>TaskItemStatus</c> (not <c>TaskStatus</c>)
/// to avoid colliding with <see cref="System.Threading.Tasks.TaskStatus"/>.
/// </summary>
public enum TaskItemStatus
{
    Todo = 0,
    InProgress = 1,
    Done = 2
}
