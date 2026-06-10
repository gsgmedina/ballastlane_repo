using TaskManager.Domain.Entities;

namespace TaskManager.Application.Abstractions;

/// <summary>
/// Persistence contract for <see cref="TaskItem"/>. Implemented in Infrastructure with raw ADO.NET.
/// </summary>
public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<TaskItem>> GetByOwnerAsync(Guid ownerUserId, CancellationToken ct = default);
    Task AddAsync(TaskItem task, CancellationToken ct = default);
    Task UpdateAsync(TaskItem task, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
