namespace TaskManager.Infrastructure.Persistence;

/// <summary>
/// Provider-agnostic database bootstrap: creates the schema/containers if they do not yet exist.
/// Implemented per data store (SQLite, Cosmos DB).
/// </summary>
public interface IDatabaseInitializer
{
    Task InitializeAsync(CancellationToken ct = default);
}
