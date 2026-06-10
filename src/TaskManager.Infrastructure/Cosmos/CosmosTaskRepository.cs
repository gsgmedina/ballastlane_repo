using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using TaskManager.Application.Abstractions;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Cosmos;

/// <summary>Cosmos DB implementation of the task repository (partitioned by /ownerUserId).</summary>
public sealed class CosmosTaskRepository : ITaskRepository
{
    private readonly Container _container;

    public CosmosTaskRepository(CosmosClient client, IOptions<CosmosOptions> options)
    {
        var o = options.Value;
        _container = client.GetContainer(o.DatabaseName, o.TasksContainer);
    }

    public async Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        // The owner (partition key) is unknown here, so query by id across partitions.
        var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id").WithParameter("@id", id.ToString());
        using var iterator = _container.GetItemQueryIterator<TaskDocument>(query);

        while (iterator.HasMoreResults)
        {
            foreach (var doc in await iterator.ReadNextAsync(ct))
                return doc.ToEntity();
        }
        return null;
    }

    public async Task<IReadOnlyList<TaskItem>> GetByOwnerAsync(Guid ownerUserId, CancellationToken ct = default)
    {
        var owner = ownerUserId.ToString();
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.ownerUserId = @owner ORDER BY c.createdAtUtc DESC")
            .WithParameter("@owner", owner);

        var options = new QueryRequestOptions { PartitionKey = new PartitionKey(owner) };
        using var iterator = _container.GetItemQueryIterator<TaskDocument>(query, requestOptions: options);

        var results = new List<TaskItem>();
        while (iterator.HasMoreResults)
        {
            foreach (var doc in await iterator.ReadNextAsync(ct))
                results.Add(doc.ToEntity());
        }
        return results;
    }

    public async Task AddAsync(TaskItem task, CancellationToken ct = default)
    {
        var doc = TaskDocument.FromEntity(task);
        await _container.CreateItemAsync(doc, new PartitionKey(doc.OwnerUserId), cancellationToken: ct);
    }

    public async Task UpdateAsync(TaskItem task, CancellationToken ct = default)
    {
        var doc = TaskDocument.FromEntity(task);
        await _container.UpsertItemAsync(doc, new PartitionKey(doc.OwnerUserId), cancellationToken: ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        // A point delete needs the partition key, so first locate the document to learn its owner.
        var existing = await GetByIdAsync(id, ct);
        if (existing is null)
            return false;

        try
        {
            await _container.DeleteItemAsync<TaskDocument>(
                id.ToString(), new PartitionKey(existing.OwnerUserId.ToString()), cancellationToken: ct);
            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }
}
