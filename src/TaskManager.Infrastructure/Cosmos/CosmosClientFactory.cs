using Microsoft.Azure.Cosmos;

namespace TaskManager.Infrastructure.Cosmos;

/// <summary>Builds a configured <see cref="CosmosClient"/> (camelCase serialization, emulator-friendly).</summary>
internal static class CosmosClientFactory
{
    public static CosmosClient Create(CosmosOptions options)
    {
        var clientOptions = new CosmosClientOptions
        {
            // Gateway mode is the most reliable against the local emulator.
            ConnectionMode = ConnectionMode.Gateway,
            SerializerOptions = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            }
        };

        if (options.BypassCertificateValidation)
        {
            // The local emulator presents a self-signed certificate.
            clientOptions.HttpClientFactory = () => new HttpClient(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });
            clientOptions.ConnectionMode = ConnectionMode.Gateway;
        }

        return new CosmosClient(options.AccountEndpoint, options.AccountKey, clientOptions);
    }
}
