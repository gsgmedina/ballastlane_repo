using Microsoft.Data.Sqlite;
using TaskManager.Application.Abstractions;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.Infrastructure.Persistence;

/// <summary>Hand-written ADO.NET repository for <see cref="TaskItem"/>.</summary>
public sealed class SqliteTaskRepository : ITaskRepository
{
    private const string Columns =
        "Id, OwnerUserId, Title, Description, Status, DueDateUtc, CreatedAtUtc, UpdatedAtUtc";

    private readonly ISqliteConnectionFactory _factory;

    public SqliteTaskRepository(ISqliteConnectionFactory factory) => _factory = factory;

    public async Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var connection = _factory.Create();
        await connection.OpenAsync(ct);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT {Columns} FROM Tasks WHERE Id = $id LIMIT 1;";
        cmd.Parameters.AddWithValue("$id", DbValue.FromGuid(id));

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task<IReadOnlyList<TaskItem>> GetByOwnerAsync(Guid ownerUserId, CancellationToken ct = default)
    {
        await using var connection = _factory.Create();
        await connection.OpenAsync(ct);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT {Columns} FROM Tasks WHERE OwnerUserId = $owner ORDER BY CreatedAtUtc DESC;";
        cmd.Parameters.AddWithValue("$owner", DbValue.FromGuid(ownerUserId));

        var results = new List<TaskItem>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            results.Add(Map(reader));

        return results;
    }

    public async Task AddAsync(TaskItem task, CancellationToken ct = default)
    {
        await using var connection = _factory.Create();
        await connection.OpenAsync(ct);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Tasks (Id, OwnerUserId, Title, Description, Status, DueDateUtc, CreatedAtUtc, UpdatedAtUtc)
            VALUES ($id, $owner, $title, $desc, $status, $due, $created, $updated);
            """;
        BindWritableParameters(cmd, task);
        cmd.Parameters.AddWithValue("$id", DbValue.FromGuid(task.Id));
        cmd.Parameters.AddWithValue("$owner", DbValue.FromGuid(task.OwnerUserId));
        cmd.Parameters.AddWithValue("$created", DbValue.FromDate(task.CreatedAtUtc));

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task UpdateAsync(TaskItem task, CancellationToken ct = default)
    {
        await using var connection = _factory.Create();
        await connection.OpenAsync(ct);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            UPDATE Tasks
            SET Title = $title, Description = $desc, Status = $status,
                DueDateUtc = $due, UpdatedAtUtc = $updated
            WHERE Id = $id;
            """;
        BindWritableParameters(cmd, task);
        cmd.Parameters.AddWithValue("$id", DbValue.FromGuid(task.Id));

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var connection = _factory.Create();
        await connection.OpenAsync(ct);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM Tasks WHERE Id = $id;";
        cmd.Parameters.AddWithValue("$id", DbValue.FromGuid(id));

        var affected = await cmd.ExecuteNonQueryAsync(ct);
        return affected > 0;
    }

    /// <summary>Binds the columns shared by INSERT and UPDATE (everything except Id/Owner/Created).</summary>
    private static void BindWritableParameters(SqliteCommand cmd, TaskItem task)
    {
        cmd.Parameters.AddWithValue("$title", task.Title);
        cmd.Parameters.AddWithValue("$desc", DbValue.OrDbNull(task.Description));
        cmd.Parameters.AddWithValue("$status", (int)task.Status);
        cmd.Parameters.AddWithValue("$due", DbValue.FromNullableDate(task.DueDateUtc));
        cmd.Parameters.AddWithValue("$updated", DbValue.FromDate(task.UpdatedAtUtc));
    }

    private static TaskItem Map(SqliteDataReader reader) => TaskItem.FromPersistence(
        DbValue.ReadGuid(reader, 0),
        DbValue.ReadGuid(reader, 1),
        reader.GetString(2),
        DbValue.ReadNullableString(reader, 3),
        (TaskItemStatus)reader.GetInt32(4),
        DbValue.ReadNullableDate(reader, 5),
        DbValue.ReadDate(reader, 6),
        DbValue.ReadDate(reader, 7));
}
