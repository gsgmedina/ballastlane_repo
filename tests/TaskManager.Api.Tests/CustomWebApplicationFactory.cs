using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskManager.Infrastructure.Persistence;

namespace TaskManager.Api.Tests;

/// <summary>
/// Boots the real API against an isolated, shared-cache in-memory SQLite database.
///
/// Two subtleties handled here:
///  1. A keep-alive connection is opened during host build (before startup seeding) so the
///     shared in-memory database survives between requests.
///  2. The SQLite connection factory is swapped via ConfigureTestServices (which runs AFTER
///     the app's own registrations) so the in-memory connection string actually takes effect.
///     The JWT signing key is deliberately left as configured so the token generator and the
///     bearer validator agree.
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString = $"Data Source=api-tests-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
    private SqliteConnection? _keepAlive;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // Keep the in-memory database alive for the lifetime of the factory.
        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ISqliteConnectionFactory>();
            services.AddSingleton<ISqliteConnectionFactory>(new SqliteConnectionFactory(_connectionString));
        });
    }

    protected override void Dispose(bool disposing)
    {
        _keepAlive?.Dispose();
        base.Dispose(disposing);
    }
}
