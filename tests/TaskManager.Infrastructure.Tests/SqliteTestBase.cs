using Microsoft.Data.Sqlite;
using TaskManager.Infrastructure.Persistence;
using Xunit;

namespace TaskManager.Infrastructure.Tests;

/// <summary>
/// Base class giving each test an isolated, schema-initialized in-memory SQLite database.
/// A keep-alive connection is held open for the lifetime of the test so the shared-cache
/// in-memory database is not destroyed between repository operations.
/// </summary>
public abstract class SqliteTestBase : IAsyncLifetime
{
    private SqliteConnection _keepAlive = null!;
    protected ISqliteConnectionFactory Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // Unique name per test instance => isolation across the test class.
        var dbName = $"tm-tests-{Guid.NewGuid():N}";
        var connectionString = $"Data Source={dbName};Mode=Memory;Cache=Shared";

        _keepAlive = new SqliteConnection(connectionString);
        await _keepAlive.OpenAsync();

        Factory = new SqliteConnectionFactory(connectionString);
        await new SqliteDatabaseInitializer(Factory).InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await _keepAlive.DisposeAsync();
    }
}
