using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using TaskManager.Application.Abstractions;
using TaskManager.Domain.Entities;

// Microsoft.Azure.Cosmos also defines a "User" type; disambiguate to our domain entity.
using User = TaskManager.Domain.Entities.User;

namespace TaskManager.Infrastructure.Cosmos;

/// <summary>Cosmos DB implementation of the user repository (partitioned by /id).</summary>
public sealed class CosmosUserRepository : IUserRepository
{
    private readonly Container _container;

    public CosmosUserRepository(CosmosClient client, IOptions<CosmosOptions> options)
    {
        var o = options.Value;
        _container = client.GetContainer(o.DatabaseName, o.UsersContainer);
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var key = id.ToString();
            var response = await _container.ReadItemAsync<UserDocument>(key, new PartitionKey(key), cancellationToken: ct);
            return response.Resource.ToEntity();
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        // Email is stored normalized to lowercase by the application layer, so an exact match works.
        var query = new QueryDefinition("SELECT * FROM c WHERE c.email = @email").WithParameter("@email", email);
        using var iterator = _container.GetItemQueryIterator<UserDocument>(query);

        while (iterator.HasMoreResults)
        {
            foreach (var doc in await iterator.ReadNextAsync(ct))
                return doc.ToEntity();
        }
        return null;
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
    {
        var query = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.email = @email")
            .WithParameter("@email", email);
        using var iterator = _container.GetItemQueryIterator<int>(query);

        while (iterator.HasMoreResults)
        {
            foreach (var count in await iterator.ReadNextAsync(ct))
                return count > 0;
        }
        return false;
    }

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        var doc = UserDocument.FromEntity(user);
        await _container.CreateItemAsync(doc, new PartitionKey(doc.Id), cancellationToken: ct);
    }
}
