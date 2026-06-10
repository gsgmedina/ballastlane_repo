using Microsoft.Data.Sqlite;
using TaskManager.Application.Abstractions;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Persistence;

/// <summary>Hand-written ADO.NET repository for <see cref="User"/>.</summary>
public sealed class SqliteUserRepository : IUserRepository
{
    private const string Columns = "Id, Email, PasswordHash, DisplayName, CreatedAtUtc";
    private readonly ISqliteConnectionFactory _factory;

    public SqliteUserRepository(ISqliteConnectionFactory factory) => _factory = factory;

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var connection = _factory.Create();
        await connection.OpenAsync(ct);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT {Columns} FROM Users WHERE Id = $id LIMIT 1;";
        cmd.Parameters.AddWithValue("$id", DbValue.FromGuid(id));

        return await ReadSingleAsync(cmd, ct);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        await using var connection = _factory.Create();
        await connection.OpenAsync(ct);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT {Columns} FROM Users WHERE Email = $email LIMIT 1;";
        cmd.Parameters.AddWithValue("$email", email);

        return await ReadSingleAsync(cmd, ct);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
    {
        await using var connection = _factory.Create();
        await connection.OpenAsync(ct);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT EXISTS(SELECT 1 FROM Users WHERE Email = $email);";
        cmd.Parameters.AddWithValue("$email", email);

        var result = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt64(result) == 1;
    }

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        await using var connection = _factory.Create();
        await connection.OpenAsync(ct);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Users (Id, Email, PasswordHash, DisplayName, CreatedAtUtc)
            VALUES ($id, $email, $hash, $name, $created);
            """;
        cmd.Parameters.AddWithValue("$id", DbValue.FromGuid(user.Id));
        cmd.Parameters.AddWithValue("$email", user.Email);
        cmd.Parameters.AddWithValue("$hash", user.PasswordHash);
        cmd.Parameters.AddWithValue("$name", user.DisplayName);
        cmd.Parameters.AddWithValue("$created", DbValue.FromDate(user.CreatedAtUtc));

        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task<User?> ReadSingleAsync(SqliteCommand cmd, CancellationToken ct)
    {
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
            return null;

        return User.FromPersistence(
            DbValue.ReadGuid(reader, 0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            DbValue.ReadDate(reader, 4));
    }
}
