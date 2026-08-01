using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.CosmosDB;
using Azure.ResourceManager.CosmosDB.Models;
using Azure.ResourceManager.Resources;
using Microsoft.Azure.Cosmos;

namespace OuterloopLabApi;

public static class CosmosProvisioning
{
    private const int DefaultThroughput = 400;
    private const string PartitionKeyPath = "/pk";

    public sealed class CosmosSettings
    {
        public string CosmosDbUri { get; init; } = string.Empty;
        public string DatabaseName { get; init; } = string.Empty;
        public string ContainerName { get; init; } = string.Empty;
        public string CosmosAccountName { get; init; } = string.Empty;
        public string CosmosResourceGroup { get; init; } = string.Empty;
        public string CosmosRegion { get; init; } = string.Empty;
        public string ManagedIdentityClientId { get; init; } = string.Empty;
    }

    public static CosmosSettings ReadFromEnvironment()
    {
        // Must use exact environment-variable keys defined in docs\CONTAINER_ENVIRONMENT_VARIABLES.md.
        return new CosmosSettings
        {
            CosmosDbUri = Environment.GetEnvironmentVariable("COSMOS_DB_URI") ?? throw new InvalidOperationException("COSMOS_DB_URI is required"),
            DatabaseName = Environment.GetEnvironmentVariable("COSMOS_DB_DATABASE") ?? throw new InvalidOperationException("COSMOS_DB_DATABASE is required"),
            ContainerName = Environment.GetEnvironmentVariable("COSMOS_DB_CONTAINER") ?? throw new InvalidOperationException("COSMOS_DB_CONTAINER is required"),
            CosmosAccountName = Environment.GetEnvironmentVariable("COSMOS_DB_ACCOUNT_NAME") ?? throw new InvalidOperationException("COSMOS_DB_ACCOUNT_NAME is required"),
            CosmosResourceGroup = Environment.GetEnvironmentVariable("COSMOS_DB_RESOURCE_GROUP") ?? throw new InvalidOperationException("COSMOS_DB_RESOURCE_GROUP is required"),
            CosmosRegion = Environment.GetEnvironmentVariable("COSMOS_DB_REGION") ?? throw new InvalidOperationException("COSMOS_DB_REGION is required"),
            ManagedIdentityClientId = Environment.GetEnvironmentVariable("AZURE_MANAGED_IDENTITY_CLIENT_ID") ?? throw new InvalidOperationException("AZURE_MANAGED_IDENTITY_CLIENT_ID is required")
        };
    }

    public static async Task<(CosmosClient client, Database database, Container container)> ProvisionDataPlaneAsync(CosmosSettings settings, CancellationToken cancellationToken)
    {
        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ManagedIdentityClientId = settings.ManagedIdentityClientId
        });

        // Data-plane: token-authenticated create-if-not-exists must succeed.
        var cosmosClient = new CosmosClient(settings.CosmosDbUri, credential);

        // Best-effort ARM provisioning is done first.
        await TryProvisionControlPlaneAsync(settings, credential, cancellationToken);

        var dbResponse = await cosmosClient.CreateDatabaseIfNotExistsAsync(settings.DatabaseName, cancellationToken: cancellationToken);
        var containerProperties = new ContainerProperties(settings.ContainerName, "/pk");
        var containerResponse = await dbResponse.Database.CreateContainerIfNotExistsAsync(containerProperties, throughput: DefaultThroughput, cancellationToken: cancellationToken);
        return (cosmosClient, dbResponse.Database, containerResponse.Container);
    }

    private static async Task TryProvisionControlPlaneAsync(CosmosSettings settings, TokenCredential credential, CancellationToken cancellationToken)
    {
        try
        {
            // Subscription id isn't part of the provided container env vars, so ARM provisioning is inherently best-effort.
            var subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
            if (string.IsNullOrWhiteSpace(subscriptionId))
                return;

            var armClient = new ArmClient(credential, subscriptionId);

            // SQL database
            var azureLocation = new AzureLocation(settings.CosmosRegion);
            var sqlDbResourceId = $"/subscriptions/{subscriptionId}/resourceGroups/{settings.CosmosResourceGroup}/providers/Microsoft.DocumentDB/databaseAccounts/{settings.CosmosAccountName}/sqlDatabases/{settings.DatabaseName}";
            var sqlDbResource = armClient.GetResource<CosmosDBSqlDatabaseResource>(new ResourceIdentifier(sqlDbResourceId));

            // Use the collection operations so CreateOrUpdateAsync exists.
            var sqlDbCollection = armClient.GetResource<CosmosDBAccountResource>(
                new ResourceIdentifier($"/subscriptions/{subscriptionId}/resourceGroups/{settings.CosmosResourceGroup}/providers/Microsoft.DocumentDB/databaseAccounts/{settings.CosmosAccountName}"))
                .GetCosmosDBSqlDatabases();

            var dbInfo = new CosmosDBSqlDatabaseResourceInfo(settings.DatabaseName);
            var dbContent = new CosmosDBSqlDatabaseCreateOrUpdateContent(azureLocation, dbInfo);
            await sqlDbCollection.CreateOrUpdateAsync(WaitUntil.Completed, settings.DatabaseName, dbContent, cancellationToken);

            // SQL container
            var sqlContainerResourceId = $"/subscriptions/{subscriptionId}/resourceGroups/{settings.CosmosResourceGroup}/providers/Microsoft.DocumentDB/databaseAccounts/{settings.CosmosAccountName}/sqlDatabases/{settings.DatabaseName}/containers/{settings.ContainerName}";
            var sqlDbContainers = armClient.GetResource<CosmosDBSqlDatabaseResource>(new ResourceIdentifier(sqlDbResourceId)).GetCosmosDBSqlContainers();

            var containerInfo = new CosmosDBSqlContainerResourceInfo(settings.ContainerName);
            var containerContent = new CosmosDBSqlContainerCreateOrUpdateContent(azureLocation, containerInfo);
            await sqlDbContainers.CreateOrUpdateAsync(WaitUntil.Completed, settings.ContainerName, containerContent, cancellationToken);
        }
        catch
        {
            // Best-effort: allow ARM RBAC mismatch.
        }
    }
}
