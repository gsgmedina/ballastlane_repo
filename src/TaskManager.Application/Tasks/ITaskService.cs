using TaskManager.Application.Tasks.Dtos;

namespace TaskManager.Application.Tasks;

/// <summary>
/// Business logic for tasks. All operations are scoped to the owning user so that
/// users can only ever see or mutate their own tasks.
/// </summary>
public interface ITaskService
{
    Task<IReadOnlyList<TaskResponse>> GetTasksAsync(Guid ownerUserId, CancellationToken ct = default);
    Task<TaskResponse> GetTaskAsync(Guid taskId, Guid ownerUserId, CancellationToken ct = default);
    Task<TaskResponse> CreateTaskAsync(Guid ownerUserId, CreateTaskRequest request, CancellationToken ct = default);
    Task<TaskResponse> UpdateTaskAsync(Guid taskId, Guid ownerUserId, UpdateTaskRequest request, CancellationToken ct = default);
    Task DeleteTaskAsync(Guid taskId, Guid ownerUserId, CancellationToken ct = default);
}
