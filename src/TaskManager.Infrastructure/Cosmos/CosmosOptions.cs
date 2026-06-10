namespace TaskManager.Infrastructure.Cosmos;

/// <summary>Configuration for the Azure Cosmos DB provider (bound from the "Cosmos" section).</summary>
public sealed class CosmosOptions
{
    public const string SectionName = "Cosmos";

    /// <summary>Account endpoint. For the local emulator this is https://localhost:8081.</summary>
    public string AccountEndpoint { get; set; } = "https://localhost:8081";

    /// <summary>Account key. Defaults to the well-known local emulator key.</summary>
    public string AccountKey { get; set; } =
        "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

    public string DatabaseName { get; set; } = "TaskManager";
    public string UsersContainer { get; set; } = "Users";
    public string TasksContainer { get; set; } = "Tasks";

    /// <summary>
    /// When true, the server certificate is not validated. Required for the local emulator's
    /// self-signed certificate; must remain false against a real Cosmos DB account.
    /// </summary>
    public bool BypassCertificateValidation { get; set; } = true;
}
