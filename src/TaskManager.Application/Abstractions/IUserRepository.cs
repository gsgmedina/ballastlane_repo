using TaskManager.Domain.Entities;

namespace TaskManager.Application.Abstractions;

/// <summary>Persistence contract for <see cref="User"/>. Implemented in Infrastructure with raw ADO.NET.</summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
}
