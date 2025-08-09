using ClearDataService.Exceptions;
using ClearDataService.Models;
using ClearDataService.Utils;
using Microsoft.Azure.Cosmos;

namespace ClearDataService.Migrators;

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

    private static async Task CreateContainer(DatabaseResponse db, CosmosDbContainerInfo container)
    {
        if (string.IsNullOrEmpty(container.Name))
        {
            throw new ContainerNameEmptyException();
        }

        await db.Database.CreateContainerIfNotExistsAsync(container.Name, container.PartitionKeyPath);
    }
}