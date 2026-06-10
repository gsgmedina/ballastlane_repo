using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using TaskManager.Infrastructure.Persistence;

namespace TaskManager.Infrastructure.Cosmos;

/// <summary>Creates the Cosmos database and containers (idempotently) if they do not exist.</summary>
public sealed class CosmosDbInitializer : IDatabaseInitializer
{
    private readonly CosmosClient _client;
    private readonly CosmosOptions _options;

    public CosmosDbInitializer(CosmosClient client, IOptions<CosmosOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        var dbResponse = await _client.CreateDatabaseIfNotExistsAsync(_options.DatabaseName, cancellationToken: ct);
        var database = dbResponse.Database;

        // Minimum manual throughput (400 RU/s) keeps it compatible with the local emulator.
        await database.CreateContainerIfNotExistsAsync(
            new ContainerProperties(_options.UsersContainer, partitionKeyPath: "/id"),
            throughput: 400, cancellationToken: ct);

        await database.CreateContainerIfNotExistsAsync(
            new ContainerProperties(_options.TasksContainer, partitionKeyPath: "/ownerUserId"),
            throughput: 400, cancellationToken: ct);
    }
}
