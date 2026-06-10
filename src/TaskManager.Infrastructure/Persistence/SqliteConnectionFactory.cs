using Microsoft.Data.Sqlite;

namespace TaskManager.Infrastructure.Persistence;

/// <summary>Creates SQLite connections from a configured connection string.</summary>
public interface ISqliteConnectionFactory
{
    SqliteConnection Create();
}

public sealed class SqliteConnectionFactory : ISqliteConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnectionFactory(string connectionString)
        => _connectionString = connectionString;

    public SqliteConnection Create() => new(_connectionString);
}
