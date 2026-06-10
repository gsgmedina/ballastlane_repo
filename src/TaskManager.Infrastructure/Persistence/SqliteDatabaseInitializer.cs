using Microsoft.Data.Sqlite;

namespace TaskManager.Infrastructure.Persistence;

/// <summary>
/// Creates the SQLite schema (idempotently) using hand-written SQL — no ORM/migrations framework.
/// </summary>
public sealed class SqliteDatabaseInitializer : IDatabaseInitializer
{
    private readonly ISqliteConnectionFactory _factory;

    public SqliteDatabaseInitializer(ISqliteConnectionFactory factory) => _factory = factory;

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await using var connection = _factory.Create();
        await connection.OpenAsync(ct);

        // Enforce foreign keys for this connection.
        await ExecuteAsync(connection, "PRAGMA foreign_keys = ON;", ct);

        const string sql = """
            CREATE TABLE IF NOT EXISTS Users (
                Id            TEXT NOT NULL PRIMARY KEY,
                Email         TEXT NOT NULL COLLATE NOCASE UNIQUE,
                PasswordHash  TEXT NOT NULL,
                DisplayName   TEXT NOT NULL,
                CreatedAtUtc  TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Tasks (
                Id            TEXT NOT NULL PRIMARY KEY,
                OwnerUserId   TEXT NOT NULL,
                Title         TEXT NOT NULL,
                Description   TEXT NULL,
                Status        INTEGER NOT NULL,
                DueDateUtc    TEXT NULL,
                CreatedAtUtc  TEXT NOT NULL,
                UpdatedAtUtc  TEXT NOT NULL,
                FOREIGN KEY (OwnerUserId) REFERENCES Users(Id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS IX_Tasks_OwnerUserId ON Tasks(OwnerUserId);
            """;

        await ExecuteAsync(connection, sql, ct);
    }

    private static async Task ExecuteAsync(SqliteConnection connection, string sql, CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
