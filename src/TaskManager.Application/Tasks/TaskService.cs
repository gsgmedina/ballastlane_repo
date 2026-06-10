using FluentValidation;
using TaskManager.Application.Abstractions;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Tasks.Dtos;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Tasks;

public sealed class TaskService : ITaskService
{
    private readonly ITaskRepository _tasks;
    private readonly IClock _clock;
    private readonly IValidator<CreateTaskRequest> _createValidator;
    private readonly IValidator<UpdateTaskRequest> _updateValidator;

    public TaskService(
        ITaskRepository tasks,
        IClock clock,
        IValidator<CreateTaskRequest> createValidator,
        IValidator<UpdateTaskRequest> updateValidator)
    {
        _tasks = tasks;
        _clock = clock;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<IReadOnlyList<TaskResponse>> GetTasksAsync(Guid ownerUserId, CancellationToken ct = default)
    {
        var items = await _tasks.GetByOwnerAsync(ownerUserId, ct);
        return items.Select(TaskResponse.FromEntity).ToList();
    }

    public async Task<TaskResponse> GetTaskAsync(Guid taskId, Guid ownerUserId, CancellationToken ct = default)
    {
        var task = await GetOwnedTaskOrThrowAsync(taskId, ownerUserId, ct);
        return TaskResponse.FromEntity(task);
    }

    public async Task<TaskResponse> CreateTaskAsync(Guid ownerUserId, CreateTaskRequest request, CancellationToken ct = default)
    {
        await _createValidator.ValidateAndThrowAsync(request, ct);

        var task = TaskItem.Create(
            ownerUserId,
            request.Title,
            request.Description,
            request.DueDateUtc,
            _clock.UtcNow,
            request.Status ?? TaskItemStatus.Todo);

        await _tasks.AddAsync(task, ct);
        return TaskResponse.FromEntity(task);
    }

    public async Task<TaskResponse> UpdateTaskAsync(Guid taskId, Guid ownerUserId, UpdateTaskRequest request, CancellationToken ct = default)
    {
        await _updateValidator.ValidateAndThrowAsync(request, ct);

        var task = await GetOwnedTaskOrThrowAsync(taskId, ownerUserId, ct);

        task.UpdateDetails(request.Title, request.Description, request.DueDateUtc, _clock.UtcNow);
        task.ChangeStatus(request.Status, _clock.UtcNow);

        await _tasks.UpdateAsync(task, ct);
        return TaskResponse.FromEntity(task);
    }

    public async Task DeleteTaskAsync(Guid taskId, Guid ownerUserId, CancellationToken ct = default)
    {
        // Ensure the task exists AND belongs to the caller before deleting.
        await GetOwnedTaskOrThrowAsync(taskId, ownerUserId, ct);
        await _tasks.DeleteAsync(taskId, ct);
    }

    private async Task<TaskItem> GetOwnedTaskOrThrowAsync(Guid taskId, Guid ownerUserId, CancellationToken ct)
    {
        var task = await _tasks.GetByIdAsync(taskId, ct);

        // Return 404 (not 403) for tasks owned by someone else so existence is not leaked.
        if (task is null || task.OwnerUserId != ownerUserId)
            throw NotFoundException.For("Task", taskId);

        return task;
    }
}
