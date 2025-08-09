using ClearDataService.Abstractions;
using ClearDataService.Entities.Cosmos;
using ClearDataService.Exceptions;
using ClearDataService.Models;
using ClearDataService.Utils;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace ClearDataService.Contexts;

public class CosmosDbContext : ICosmosDbContext
{
    private readonly CosmosClient _client;
    private readonly ICosmosDbSettings _settings;
    private readonly Dictionary<string, ContainerBatchBuffer> _containerBuffers;

    public CosmosDbContext(CosmosClient client, ICosmosDbSettings settings)
    {
        _client = client;
        _settings = settings;
        _containerBuffers = [];
    }

    private static PartitionKey GetPartitionKey(string? partitionKey)
    {
        return string.IsNullOrWhiteSpace(partitionKey) ? PartitionKey.None : new PartitionKey(partitionKey);
    }

    private Container GetContainer(string containerName)
    {
        if (string.IsNullOrWhiteSpace(containerName))
        {
            throw new ContainerNameEmptyException();
        }
        return _client.GetContainer(_settings.DatabaseName, containerName);
    }

    public async Task<T> Get<T>(string containerName, string id,
        string? partitionKey = null, CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity
    {
        var query = this.GetAsQueryable<T>(containerName, partitionKey);
        return (await query.ToResult(cancellationToken)).Select(x => x.Data).First(x => x.Id == id);
    }

    public async Task<T?> Get<T>(string containerName, Func<T, bool> predicate,
        string? partitionKey = null, CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity
    {
        var query = this.GetAsQueryable<T>(containerName, partitionKey);
        return (await query.ToResult(cancellationToken)).Select(x => x.Data).FirstOrDefault(predicate);
    }

    public async Task<List<T>> GetList<T>(
        string containerName, string? partitionKey = null,
        CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity
    {
        var query = this.GetAsQueryable<T>(containerName, partitionKey);
        return (await query.ToResult(cancellationToken)).Select(x => x.Data).ToList();
    }

    public async Task<List<T>> GetList<T>(string containerName, Func<T, bool> predicate,
        string? partitionKey = null, CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity
    {
        var query = this.GetAsQueryable<T>(containerName, partitionKey);
        return (await query.ToResult(cancellationToken)).Select(x => x.Data).Where(predicate).ToList();
    }

    // ======================================================

    public async Task<CosmosDbDocument<T>> GetDocument<T>(string containerName,
        string id, string? partitionKey = null, CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity
    {
        var query = this.GetAsQueryable<T>(containerName, partitionKey);
        return (await query.ToResult(cancellationToken)).First(x => x.Id == id);
    }

    public async Task<CosmosDbDocument<T>?> GetDocument<T>(string containerName,
        Func<CosmosDbDocument<T>, bool> predicate, string? partitionKey = null,
        CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity
    {
        var query = this.GetAsQueryable<T>(containerName, partitionKey);
        return (await query.ToResult(cancellationToken)).FirstOrDefault(predicate);
    }

    public async Task<List<CosmosDbDocument<T>>> GetDocuments<T>(
        string containerName, string? partitionKey = null,
        CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity
    {
        var query = this.GetAsQueryable<T>(containerName, partitionKey);
        return await query.ToResult(cancellationToken);
    }

    public async Task<List<CosmosDbDocument<T>>> GetDocuments<T>(string containerName,
        Func<CosmosDbDocument<T>, bool> predicate,
        string? partitionKey = null, CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity
    {
        var query = this.GetAsQueryable<T>(containerName, partitionKey);
        return (await query.ToResult(cancellationToken)).Where(predicate).ToList();
    }

    // ======================================================

    public IQueryable<CosmosDbDocument<T>> GetAsQueryable<T>(string containerName, Expression<Func<CosmosDbDocument<T>, bool>> predicate, string? partitionKey = null) where T : ICosmosDbEntity
    {
        return GetAsQueryable<T>(containerName, partitionKey).Where(predicate);
    }

    private IQueryable<CosmosDbDocument<T>> GetAsQueryable<T>(string containerName, string? partitionKey = null) where T : ICosmosDbEntity
    {
        var container = GetContainer(containerName);
        var queryable = container.GetItemLinqQueryable<CosmosDbDocument<T>>(
            linqSerializerOptions: new CosmosLinqSerializerOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            }
            ,
            requestOptions: string.IsNullOrEmpty(partitionKey) ? null : new QueryRequestOptions
            {
                PartitionKey = GetPartitionKey(partitionKey)
            }
        );

        return queryable;
    }

    // ======================================================

    public async Task<CosmosDbDocument<T>> Save<T>(string containerName, T entity, string partitionKey) where T : ICosmosDbEntity
    {
        var container = GetContainer(containerName);
        var doc = CosmosDbDocument<T>.Create(entity, partitionKey);
        var response = await container.CreateItemAsync(doc);
        return response.Resource;
    }

    public async Task<CosmosDbDocument<T>> Upsert<T>(string containerName, T entity, string partitionKey) where T : ICosmosDbEntity
    {
        var container = GetContainer(containerName);
        var doc = CosmosDbDocument<T>.Create(entity, partitionKey);
        var response = await container.UpsertItemAsync(doc);
        return response.Resource;
    }

    public async Task Delete<T>(string containerName, string id, string? partitionKey = null)
    {
        var container = GetContainer(containerName);
        await container.DeleteItemAsync<T>(id, GetPartitionKey(partitionKey));
    }

    // ======================================================

    public void AddToBatch<T>(string containerName, T item, string partitionKey) where T : ICosmosDbEntity
    {
        if (string.IsNullOrWhiteSpace(partitionKey))
        {
            throw new PartitionKeyNullException();
        }

        if (item is null) throw new ArgumentNullException(nameof(item));

        if (!_containerBuffers.TryGetValue(containerName, out var buffer))
        {
            buffer = new ContainerBatchBuffer(GetContainer(containerName));
            _containerBuffers[containerName] = buffer;
        }

        buffer.AddItem(partitionKey, item);
    }

    public async Task<List<CosmosBatchResult>> SaveBatchAsync()
    {
        var batchResults = new List<CosmosBatchResult>();

        foreach (var buffer in _containerBuffers.Values)
        {
            var container = buffer.Container;

            foreach (var kvp in buffer.PartitionedItems)
            {
                var partitionKey = kvp.Key;
                var items = kvp.Value;

                var batch = container.CreateTransactionalBatch(GetPartitionKey(partitionKey));

                foreach (var item in items)
                {
                    batch.CreateItem(item);
                }

                try
                {
                    TransactionalBatchResponse response = await batch.ExecuteAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        batchResults.Add(CosmosBatchResult.Success(
                            container.Id, partitionKey, response.StatusCode
                        ));
                    }
                    else
                    {
                        batchResults.Add(CosmosBatchResult.Failure(
                            container.Id, partitionKey, response.StatusCode, response.ErrorMessage
                        ));
                    }
                }
                catch (CosmosException ex)
                {
                    batchResults.Add(CosmosBatchResult.Failure(
                        container.Id, partitionKey, ex.StatusCode, ex.Message
                    ));
                }
                catch (Exception ex)
                {
                    batchResults.Add(CosmosBatchResult.Failure(
                        container.Id, partitionKey, null, ex.Message
                    ));
                }
            }
        }

        _containerBuffers.Clear();

        return batchResults;
    }
}

public static class QueryExtensions
{
    public static async Task<List<CosmosDbDocument<T>>> ToResult<T>(
        this IQueryable<CosmosDbDocument<T>> query,
        CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity
    {
        var results = new List<CosmosDbDocument<T>>();

        using var resultSetIterator = query.ToFeedIterator();
        while (resultSetIterator.HasMoreResults)
        {
            var response = await resultSetIterator.ReadNextAsync(cancellationToken);
            results.AddRange([.. response]);
        }

        return results;
    }
}