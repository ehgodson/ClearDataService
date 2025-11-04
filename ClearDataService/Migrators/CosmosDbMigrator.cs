using Clear.DataService.Exceptions;
using Clear.DataService.Models;
using Clear.DataService.Utils;
using Microsoft.Azure.Cosmos;

namespace Clear.DataService.Migrators;

public interface ICosmosDbMigrator
{
    Task CreateDatabaseAndContainers(IEnumerable<CosmosDbContainerInfo> containers);
}

public class CosmosDbMigrator : ICosmosDbMigrator
{
    private readonly CosmosClient _client;
    private readonly ICosmosDbSettings _settings;

    public CosmosDbMigrator(CosmosClient client, ICosmosDbSettings settings)
    {
        _client = client;
        _settings = settings;
    }

    public async Task CreateDatabaseAndContainers(IEnumerable<CosmosDbContainerInfo> containers)
    {
        var db = await _client.CreateDatabaseIfNotExistsAsync(_settings.DatabaseName);
        foreach (var container in containers)
        {
            await CreateContainer(db, container);
        }
    }

    private static async Task CreateContainer(DatabaseResponse db, CosmosDbContainerInfo containerInfo)
    {
        if (string.IsNullOrEmpty(containerInfo.Name))
        {
            throw new ContainerNameEmptyException();
        }

        // Check if container uses hierarchical partition keys
        if (containerInfo.IsHierarchical)
        {
            // Create container with hierarchical partition key definition
            var containerProperties = new ContainerProperties(containerInfo.Name, containerInfo.PartitionKeyPaths);

            // Set partition key version to V2 for hierarchical support
            containerProperties.PartitionKeyDefinitionVersion = PartitionKeyDefinitionVersion.V2;

            await db.Database.CreateContainerIfNotExistsAsync(containerProperties);
        }
        else
        {
            // Create container with single partition key (traditional approach)
            await db.Database.CreateContainerIfNotExistsAsync(containerInfo.Name, containerInfo.PartitionKeyPath);
        }
    }
}