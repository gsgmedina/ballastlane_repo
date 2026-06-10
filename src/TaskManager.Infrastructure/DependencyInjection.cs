using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.Application.Abstractions;
using TaskManager.Infrastructure.Cosmos;
using TaskManager.Infrastructure.Persistence;
using TaskManager.Infrastructure.Security;
using TaskManager.Infrastructure.Time;

namespace TaskManager.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Wires the concrete data-access, security and time implementations behind Application abstractions.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Select the data store: "Sqlite" (default) or "Cosmos".
        var provider = configuration["Database:Provider"] ?? "Sqlite";
        if (provider.Equals("Cosmos", StringComparison.OrdinalIgnoreCase))
            AddCosmos(services, configuration);
        else
            AddSqlite(services, configuration);

        // ---- Cross-cutting services (provider-independent) ----
        services.AddScoped<DataSeeder>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IClock, SystemClock>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }

    private static void AddSqlite(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default") ?? "Data Source=taskmanager.db";

        services.AddSingleton<ISqliteConnectionFactory>(new SqliteConnectionFactory(connectionString));
        services.AddSingleton<IDatabaseInitializer, SqliteDatabaseInitializer>();
        services.AddScoped<ITaskRepository, SqliteTaskRepository>();
        services.AddScoped<IUserRepository, SqliteUserRepository>();
    }

    private static void AddCosmos(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CosmosOptions>(configuration.GetSection(CosmosOptions.SectionName));

        // A single CosmosClient instance should be reused for the lifetime of the application.
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<CosmosOptions>>().Value;
            return CosmosClientFactory.Create(options);
        });

        services.AddSingleton<IDatabaseInitializer, CosmosDbInitializer>();
        services.AddScoped<ITaskRepository, CosmosTaskRepository>();
        services.AddScoped<IUserRepository, CosmosUserRepository>();
    }
}
