using Clear.DataService.Abstractions;
using Clear.DataService.Entities.Cosmos;
using Clear.DataService.Exceptions;
using Clear.DataService.Models;
using Clear.DataService.Utils;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using System.Linq.Expressions;

namespace Clear.DataService.Contexts;

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

    /// <summary>
    /// Converts a HierarchicalPartitionKey to Cosmos DB PartitionKey
    /// </summary>
    private static PartitionKey GetPartitionKey(HierarchicalPartitionKey? hierarchicalKey)
    {
        return hierarchicalKey?.ToCosmosPartitionKey() ?? PartitionKey.None;
    }

    /// <summary>
    /// Overloaded method to handle both string and hierarchical partition keys
    /// </summary>
    private static PartitionKey GetPartitionKey(object? partitionKey)
    {
        return partitionKey switch
        {
            null => PartitionKey.None,
            string str => GetPartitionKey(str),
            HierarchicalPartitionKey hierarchical => GetPartitionKey(hierarchical),
            _ => throw new ArgumentException($"Unsupported partition key type: {partitionKey.GetType()}")
        };
    }

    private Microsoft.Azure.Cosmos.Container GetContainer(string containerName)
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
    // Pagination Methods
    // ======================================================

    public async Task<PagedResult<T>> GetPagedList<T>(
        string containerName,
        int pageSize = 100,
        string? continuationToken = null,
        string? partitionKey = null,
        CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity
    {
        var query = GetAsQueryable<T>(containerName, partitionKey);
        var pagedResult = await query.ToPagedResult(pageSize, continuationToken, cancellationToken);

        return new PagedResult<T>
        {
            Items = pagedResult.Items.Select(x => x.Data).ToList(),
            ContinuationToken = pagedResult.ContinuationToken,
            HasMoreResults = pagedResult.HasMoreResults,
            Count = pagedResult.Count,
            RequestCharge = pagedResult.RequestCharge
        };
    }

    public async Task<PagedResult<T>> GetPagedList<T>(
        string containerName,
        Expression<Func<T, bool>> predicate,
        int pageSize = 100,
        string? continuationToken = null,
        string? partitionKey = null,
        CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity
    {
        // Convert predicate from T to CosmosDbDocument<T>
        var parameter = Expression.Parameter(typeof(CosmosDbDocument<T>), "doc");
        var dataProperty = Expression.Property(parameter, nameof(CosmosDbDocument<T>.Data));
        var convertedPredicate = Expression.Lambda<Func<CosmosDbDocument<T>, bool>>(
            Expression.Invoke(predicate, dataProperty),
            parameter
        );

        var query = GetAsQueryable<T>(containerName, convertedPredicate, partitionKey);
        var pagedResult = await query.ToPagedResult(pageSize, continuationToken, cancellationToken);

        return new PagedResult<T>
        {
            Items = pagedResult.Items.Select(x => x.Data).ToList(),
            ContinuationToken = pagedResult.ContinuationToken,
            HasMoreResults = pagedResult.HasMoreResults,
            Count = pagedResult.Count,
            RequestCharge = pagedResult.RequestCharge
        };
    }

    public async Task<PagedCosmosResult<T>> GetPagedDocuments<T>(
        string containerName,
        int pageSize = 100,
        string? continuationToken = null,
        string? partitionKey = null,
        CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity
    {
        var query = GetAsQueryable<T>(containerName, partitionKey);
        return await query.ToPagedResult(pageSize, continuationToken, cancellationToken);
    }

    public async Task<PagedCosmosResult<T>> GetPagedDocuments<T>(
        string containerName,
        Expression<Func<CosmosDbDocument<T>, bool>> predicate,
        int pageSize = 100,
        string? continuationToken = null,
        string? partitionKey = null,
        CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity
    {
        var query = GetAsQueryable<T>(containerName, predicate, partitionKey);
        return await query.ToPagedResult(pageSize, continuationToken, cancellationToken);
    }

    // ======================================================
    // Advanced Pagination Methods with SQL Support
    // ======================================================

    /// <summary>
    /// Gets a paged list using SQL query with proper continuation token support
    /// </summary>
    public async Task<PagedResult<T>> GetPagedListWithSql<T>(
        string containerName,
        string whereClause = "",
        Dictionary<string, object>? parameters = null,
        int pageSize = 100,
        string? continuationToken = null,
        string? partitionKey = null,
        CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity
    {
        var container = GetContainer(containerName);
        var entityType = typeof(T).Name;
        
        var sql = $"SELECT * FROM c WHERE c.entityType = '{entityType}'";
        if (!string.IsNullOrEmpty(whereClause))
        {
            sql += $" AND ({whereClause})";
        }

        var queryDefinition = new QueryDefinition(sql);
        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                queryDefinition.WithParameter($"@{param.Key}", param.Value);
            }
        }

        var pagedResult = await container.ToPagedResultWithSql<T>(
            sql, queryDefinition, pageSize, continuationToken, partitionKey, cancellationToken);

        return new PagedResult<T>
        {
            Items = pagedResult.Items.Select(x => x.Data).ToList(),
            ContinuationToken = pagedResult.ContinuationToken,
            HasMoreResults = pagedResult.HasMoreResults,
            Count = pagedResult.Count,
            RequestCharge = pagedResult.RequestCharge
        };
    }

    /// <summary>
    /// Gets paged documents using SQL query with proper continuation token support
    /// </summary>
    public async Task<PagedCosmosResult<T>> GetPagedDocumentsWithSql<T>(
        string containerName,
        string whereClause = "",
        Dictionary<string, object>? parameters = null,
        int pageSize = 100,
        string? continuationToken = null,
        string? partitionKey = null,
        CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity
    {
        var container = GetContainer(containerName);
        var entityType = typeof(T).Name;
        
        var sql = $"SELECT * FROM c WHERE c.entityType = '{entityType}'";
        if (!string.IsNullOrEmpty(whereClause))
        {
            sql += $" AND ({whereClause})";
        }

        var queryDefinition = new QueryDefinition(sql);
        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                queryDefinition.WithParameter($"@{param.Key}", param.Value);
            }
        }

        return await container.ToPagedResultWithSql<T>(
            sql, queryDefinition, pageSize, continuationToken, partitionKey, cancellationToken);
    }

    // ======================================================
    // GetAsQueryable Methods - Public interface implementations
    // ======================================================

    public IQueryable<CosmosDbDocument<T>> GetAsQueryable<T>(
        string containerName,
        string? partitionKey = null
    ) where T : ICosmosDbEntity
    {
        var container = GetContainer(containerName);
        var queryable = container.GetItemLinqQueryable<CosmosDbDocument<T>>(
            linqSerializerOptions: new CosmosLinqSerializerOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            },
            requestOptions: new QueryRequestOptions
            {
                PartitionKey = GetPartitionKey(partitionKey)
            }
        );

        return queryable;
    }

    public IQueryable<CosmosDbDocument<T>> GetAsQueryable<T>(
        string containerName,
        Expression<Func<CosmosDbDocument<T>, bool>> predicate,
        string? partitionKey = null
    ) where T : ICosmosDbEntity
    {
        return GetAsQueryable<T>(containerName, partitionKey).Where(predicate);
    }

    // ======================================================
    // Hierarchical Partition Key Support
    // ======================================================

    public async Task<T> Get<T>(
        string containerName, 
        string id, 
        HierarchicalPartitionKey hierarchicalPartitionKey, 
        CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity
    {
        var query = this.GetAsQueryableWithHierarchicalKey<T>(containerName, hierarchicalPartitionKey);
        return (await query.ToResult(cancellationToken)).Select(x => x.Data).First(x => x.Id == id);
    }

    public async Task<List<T>> GetList<T>(
        string containerName, 
        HierarchicalPartitionKey hierarchicalPartitionKey, 
        CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity
    {
        var query = this.GetAsQueryableWithHierarchicalKey<T>(containerName, hierarchicalPartitionKey);
        return (await query.ToResult(cancellationToken)).Select(x => x.Data).ToList();
    }

    public async Task<List<T>> GetList<T>(
        string containerName, 
        Expression<Func<T, bool>> predicate, 
        HierarchicalPartitionKey hierarchicalPartitionKey, 
        CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity
    {
        // Convert predicate from T to CosmosDbDocument<T>
        var parameter = Expression.Parameter(typeof(CosmosDbDocument<T>), "doc");
        var dataProperty = Expression.Property(parameter, nameof(CosmosDbDocument<T>.Data));
        var convertedPredicate = Expression.Lambda<Func<CosmosDbDocument<T>, bool>>(
            Expression.Invoke(predicate, dataProperty),
            parameter
        );

        var query = GetAsQueryableWithHierarchicalKey<T>(containerName, convertedPredicate, hierarchicalPartitionKey);
        return (await query.ToResult(cancellationToken)).Select(x => x.Data).ToList();
    }

    // ======================================================

    public async Task<PagedResult<T>> GetPagedList<T>(
        string containerName,
        int pageSize,
        HierarchicalPartitionKey hierarchicalPartitionKey,
        string? continuationToken = null,
        CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity
    {
        var query = GetAsQueryableWithHierarchicalKey<T>(containerName, hierarchicalPartitionKey);
        var pagedResult = await query.ToPagedResult(pageSize, continuationToken, cancellationToken);

        return new PagedResult<T>
        {
            Items = pagedResult.Items.Select(x => x.Data).ToList(),
            ContinuationToken = pagedResult.ContinuationToken,
            HasMoreResults = pagedResult.HasMoreResults,
            Count = pagedResult.Count,
            RequestCharge = pagedResult.RequestCharge
        };
    }

    public async Task<PagedResult<T>> GetPagedList<T>(
        string containerName,
        Expression<Func<T, bool>> predicate,
        int pageSize,
        HierarchicalPartitionKey hierarchicalPartitionKey,
        string? continuationToken = null,
        CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity
    {
        // Convert predicate from T to CosmosDbDocument<T>
        var parameter = Expression.Parameter(typeof(CosmosDbDocument<T>), "doc");
        var dataProperty = Expression.Property(parameter, nameof(CosmosDbDocument<T>.Data));
        var convertedPredicate = Expression.Lambda<Func<CosmosDbDocument<T>, bool>>(
            Expression.Invoke(predicate, dataProperty),
            parameter
        );

        var query = GetAsQueryableWithHierarchicalKey<T>(containerName, convertedPredicate, hierarchicalPartitionKey);
        var pagedResult = await query.ToPagedResult(pageSize, continuationToken, cancellationToken);

        return new PagedResult<T>
        {
            Items = pagedResult.Items.Select(x => x.Data).ToList(),
            ContinuationToken = pagedResult.ContinuationToken,
            HasMoreResults = pagedResult.HasMoreResults,
            Count = pagedResult.Count,
            RequestCharge = pagedResult.RequestCharge
        };
    }

    public async Task<CosmosDbDocument<T>> Save<T>(
        string containerName, 
        T entity, 
        HierarchicalPartitionKey hierarchicalPartitionKey
    ) where T : ICosmosDbEntity
    {
        var container = GetContainer(containerName);
        var doc = CosmosDbDocument<T>.Create(entity, hierarchicalPartitionKey.ToString());
        var response = await container.CreateItemAsync(doc, hierarchicalPartitionKey.ToCosmosPartitionKey());
        return response.Resource;
    }

    public async Task<CosmosDbDocument<T>> Upsert<T>(
        string containerName, 
        T entity, 
        HierarchicalPartitionKey hierarchicalPartitionKey
    ) where T : ICosmosDbEntity
    {
        var container = GetContainer(containerName);
        var doc = CosmosDbDocument<T>.Create(entity, hierarchicalPartitionKey.ToString());
        var response = await container.UpsertItemAsync(doc, hierarchicalPartitionKey.ToCosmosPartitionKey());
        return response.Resource;
    }

    public async Task Delete<T>(
        string containerName, 
        string id, 
        HierarchicalPartitionKey hierarchicalPartitionKey
    )
    {
        var container = GetContainer(containerName);
        await container.DeleteItemAsync<T>(id, hierarchicalPartitionKey.ToCosmosPartitionKey());
    }

    private IQueryable<CosmosDbDocument<T>> GetAsQueryableWithHierarchicalKey<T>(
        string containerName, 
        HierarchicalPartitionKey hierarchicalPartitionKey
    ) where T : ICosmosDbEntity
    {
        var container = GetContainer(containerName);
        var queryable = container.GetItemLinqQueryable<CosmosDbDocument<T>>(
            linqSerializerOptions: new CosmosLinqSerializerOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            },
            requestOptions: new QueryRequestOptions
            {
                PartitionKey = hierarchicalPartitionKey.ToCosmosPartitionKey()
            }
        );

        return queryable;
    }

    private IQueryable<CosmosDbDocument<T>> GetAsQueryableWithHierarchicalKey<T>(
        string containerName, 
        Expression<Func<CosmosDbDocument<T>, bool>> predicate, 
        HierarchicalPartitionKey hierarchicalPartitionKey
    ) where T : ICosmosDbEntity
    {
        return GetAsQueryableWithHierarchicalKey<T>(containerName, hierarchicalPartitionKey).Where(predicate);
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

    /// <summary>
    /// Converts a queryable to a paged result with continuation token support
    /// </summary>
    public static async Task<PagedCosmosResult<T>> ToPagedResult<T>(
        this IQueryable<CosmosDbDocument<T>> query,
        int maxItemCount = 100,
        string? continuationToken = null,
        CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity
    {
        // Note: LINQ queryable doesn't directly support continuation tokens or max item count
        // For proper continuation token support, use the SQL-based methods instead
        using var resultSetIterator = query.ToFeedIterator();

        if (resultSetIterator.HasMoreResults)
        {
            var response = await resultSetIterator.ReadNextAsync(cancellationToken);
            
            return PagedCosmosResult<T>.Create(
                response,
                response.ContinuationToken,
                response.RequestCharge
            );
        }

        return PagedCosmosResult<T>.Empty();
    }

    /// <summary>
    /// Alternative method for better continuation token support using SQL queries
    /// This should be used when you need proper continuation token handling across requests
    /// </summary>
    public static async Task<PagedCosmosResult<T>> ToPagedResultWithSql<T>(
        this Container container,
        string sqlQuery,
        QueryDefinition queryDefinition,
        int maxItemCount = 100,
        string? continuationToken = null,
        string? partitionKey = null,
        CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity
    {
        var requestOptions = new QueryRequestOptions
        {
            MaxItemCount = maxItemCount
        };

        if (!string.IsNullOrEmpty(partitionKey))
        {
            requestOptions.PartitionKey = string.IsNullOrWhiteSpace(partitionKey) 
                ? PartitionKey.None 
                : new PartitionKey(partitionKey);
        }

        using var resultSetIterator = container.GetItemQueryIterator<CosmosDbDocument<T>>(
            queryDefinition,
            continuationToken,
            requestOptions);

        if (resultSetIterator.HasMoreResults)
        {
            var response = await resultSetIterator.ReadNextAsync(cancellationToken);
            
            return PagedCosmosResult<T>.Create(
                response.Resource,
                response.ContinuationToken,
                response.RequestCharge
            );
        }

        return PagedCosmosResult<T>.Empty();
    }
}