using Clear.DataService.Abstractions;
using Clear.DataService.Entities.Cosmos;
using Clear.DataService.Exceptions;
using Clear.DataService.Models;
using Clear.DataService.Utils;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace Clear.DataService.Contexts;

public class CosmosDbContext : ICosmosDbContext
{
    private readonly CosmosClient _client;
    private readonly ICosmosDbSettings _settings;
    private readonly Dictionary<string, IContainerBatchBuffer> _containerBuffers;

    public CosmosDbContext(CosmosClient client, ICosmosDbSettings settings)
    {
        _client = client;
        _settings = settings;
        _containerBuffers = [];
    }

    /// <summary>
    /// Converts a HierarchicalPartitionKey to Cosmos DB PartitionKey
    /// </summary>
    private static PartitionKey GetPartitionKey(CosmosDbPartitionKey? hierarchicalKey)
    {
        return hierarchicalKey?.ToCosmosPartitionKey() ?? PartitionKey.None;
    }

    private Container GetContainer(string containerName)
    {
        if (string.IsNullOrWhiteSpace(containerName))
        {
            throw new ContainerNameEmptyException();
        }
        return _client.GetContainer(_settings.DatabaseName, containerName);
    }

    // ============================================
    // GET OPERATIONS - ID-based and filter-based separate
    // ============================================

    public async Task<T> Get<T>(
        string containerName,
        string id,
        CosmosDbPartitionKey? partitionKey = null,
        CancellationToken cancellationToken = default
    )
        where T : ICosmosDbEntity
    {
        var query = GetAsQueryable<T>(containerName, partitionKey);
        return (await query.ToResult(cancellationToken)).Select(x => x.Data).First(x => x.Id == id);
    }

    public async Task<T?> Get<T>(
        string containerName,
        FilterBuilder<T> filter,
        CosmosDbPartitionKey? partitionKey = null,
        CancellationToken cancellationToken = default
    )
        where T : class, ICosmosDbEntity
    {
        var query = GetAsQueryable<T>(containerName, partitionKey, filter);
        var result = await query.ToResult(cancellationToken);
        return result.FirstOrDefault()?.Data;
    }

    public async Task<List<T>> GetList<T>(
        string containerName,
        CosmosDbPartitionKey? partitionKey = null,
        FilterBuilder<T>? filter = null,
        SortBuilder<T>? sortBuilder = null,
        CancellationToken cancellationToken = default
    )
        where T : ICosmosDbEntity
    {
        var query = GetAsQueryable<T>(containerName, partitionKey, filter);

        // Apply sorting if provided
        IQueryable<CosmosDbDocument<T>> orderedQuery =
            sortBuilder?.HasSortCriteria == true ? sortBuilder.ApplyTo(query) : query;

        return (await orderedQuery.ToResult(cancellationToken)).Select(x => x.Data).ToList();
    }

    // ============================================
    // DOCUMENT OPERATIONS - ID-based and filter-based separate
    // ============================================

    public async Task<CosmosDbDocument<T>> GetDocument<T>(
        string containerName,
        string id,
        CosmosDbPartitionKey? partitionKey = null,
        CancellationToken cancellationToken = default
    )
        where T : ICosmosDbEntity
    {
        var query = GetAsQueryable<T>(containerName, partitionKey);
        return (await query.ToResult(cancellationToken)).First(x => x.Id == id);
    }

    public async Task<CosmosDbDocument<T>?> GetDocument<T>(
        string containerName,
        FilterBuilder<T> filter,
        CosmosDbPartitionKey? partitionKey = null,
        CancellationToken cancellationToken = default
    )
        where T : ICosmosDbEntity
    {
        var query = GetAsQueryable<T>(containerName, partitionKey, filter);
        var result = await query.ToResult(cancellationToken);
        return result.FirstOrDefault();
    }

    public async Task<List<CosmosDbDocument<T>>> GetDocuments<T>(
        string containerName,
        CosmosDbPartitionKey? partitionKey = null,
        FilterBuilder<T>? filter = null,
        SortBuilder<T>? sortBuilder = null,
        CancellationToken cancellationToken = default
    )
        where T : ICosmosDbEntity
    {
        var query = GetAsQueryable<T>(containerName, partitionKey, filter);

        // Apply sorting if provided
        IQueryable<CosmosDbDocument<T>> orderedQuery =
            sortBuilder?.HasSortCriteria == true ? sortBuilder.ApplyTo(query) : query;

        return await orderedQuery.ToResult(cancellationToken);
    }

    // ============================================
    // PAGINATION OPERATIONS - Consolidated
    // ============================================

    public async Task<PagedResult<T>> GetPagedList<T>(
        string containerName,
        int pageSize = 100,
        string? continuationToken = null,
        CosmosDbPartitionKey? partitionKey = null,
        FilterBuilder<T>? filter = null,
        SortBuilder<T>? sortBuilder = null,
        CancellationToken cancellationToken = default
    )
        where T : ICosmosDbEntity
    {
        var query = GetAsQueryable<T>(containerName, partitionKey, filter);

        // Apply sorting if provided
        var sortedQuery =
            sortBuilder?.HasSortCriteria == true
                ? sortBuilder.ApplyTo(query)
                : query.OrderBy(x => 1);

        var pagedResult = await sortedQuery.ToPagedResult(
            pageSize,
            continuationToken,
            cancellationToken
        );

        return new PagedResult<T>
        {
            Items = pagedResult.Items.Select(x => x.Data).ToList(),
            ContinuationToken = pagedResult.ContinuationToken,
            HasMoreResults = pagedResult.HasMoreResults,
            Count = pagedResult.Count,
            RequestCharge = pagedResult.RequestCharge,
        };
    }

    public async Task<PagedCosmosResult<T>> GetPagedDocuments<T>(
        string containerName,
        int pageSize = 100,
        string? continuationToken = null,
        CosmosDbPartitionKey? partitionKey = null,
        FilterBuilder<T>? filter = null,
        SortBuilder<T>? sortBuilder = null,
        CancellationToken cancellationToken = default
    )
        where T : ICosmosDbEntity
    {
        var query = GetAsQueryable<T>(containerName, partitionKey, filter);

        // Apply sorting if provided
        var sortedQuery =
            sortBuilder?.HasSortCriteria == true
                ? sortBuilder.ApplyTo(query)
                : query.OrderBy(x => 1);

        return await sortedQuery.ToPagedResult(pageSize, continuationToken, cancellationToken);
    }

    // ============================================
    // SQL-BASED PAGINATION - Consolidated
    // ============================================

    public async Task<PagedResult<T>> GetPagedListWithSql<T>(
        string containerName,
        string whereClause = "",
        Dictionary<string, object>? parameters = null,
        int pageSize = 100,
        string? continuationToken = null,
        CosmosDbPartitionKey? partitionKey = null,
        SortBuilder<T>? sortBuilder = null,
        CancellationToken cancellationToken = default
    )
        where T : ICosmosDbEntity
    {
        var container = GetContainer(containerName);
        var entityType = typeof(T).Name;

        var sql = $"SELECT * FROM c WHERE c.entityType = '{entityType}'";
        if (!string.IsNullOrEmpty(whereClause))
        {
            sql += $" AND ({whereClause})";
        }

        if (sortBuilder?.HasSortCriteria == true)
        {
            var orderByClause = sortBuilder.ToSqlOrderBy(propertyName =>
                $"c.data.{char.ToLowerInvariant(propertyName[0])}{propertyName[1..]}"
            );
            if (!string.IsNullOrEmpty(orderByClause))
            {
                sql += $" {orderByClause}";
            }
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
            queryDefinition,
            pageSize,
            continuationToken,
            partitionKey,
            cancellationToken
        );

        return new PagedResult<T>
        {
            Items = pagedResult.Items.Select(x => x.Data).ToList(),
            ContinuationToken = pagedResult.ContinuationToken,
            HasMoreResults = pagedResult.HasMoreResults,
            Count = pagedResult.Count,
            RequestCharge = pagedResult.RequestCharge,
        };
    }

    public async Task<PagedCosmosResult<T>> GetPagedDocumentsWithSql<T>(
        string containerName,
        string whereClause = "",
        Dictionary<string, object>? parameters = null,
        int pageSize = 100,
        string? continuationToken = null,
        CosmosDbPartitionKey? partitionKey = null,
        SortBuilder<T>? sortBuilder = null,
        CancellationToken cancellationToken = default
    )
        where T : ICosmosDbEntity
    {
        var container = GetContainer(containerName);
        var entityType = typeof(T).Name;

        var sql = $"SELECT * FROM c WHERE c.entityType = '{entityType}'";
        if (!string.IsNullOrEmpty(whereClause))
        {
            sql += $" AND ({whereClause})";
        }

        if (sortBuilder?.HasSortCriteria == true)
        {
            var orderByClause = sortBuilder.ToSqlOrderBy(propertyName =>
                $"c.data.{char.ToLowerInvariant(propertyName[0])}{propertyName[1..]}"
            );
            if (!string.IsNullOrEmpty(orderByClause))
            {
                sql += $" {orderByClause}";
            }
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
            queryDefinition,
            pageSize,
            continuationToken,
            partitionKey,
            cancellationToken
        );
    }

    // ============================================
    // QUERYABLE OPERATIONS - Consolidated
    // ============================================

    public IQueryable<CosmosDbDocument<T>> GetAsQueryable<T>(
        string containerName,
        CosmosDbPartitionKey? partitionKey = null,
        FilterBuilder<T>? filter = null
    )
        where T : ICosmosDbEntity
    {
        var container = GetContainer(containerName);
        var entityType = typeof(T).Name;

        IQueryable<CosmosDbDocument<T>> queryable = container.GetItemLinqQueryable<
            CosmosDbDocument<T>
        >(
            linqSerializerOptions: new CosmosLinqSerializerOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
            },
            requestOptions: new QueryRequestOptions { PartitionKey = GetPartitionKey(partitionKey) }
        );

        // IMPORTANT: Filter by entity type to ensure we only get documents of type T
        queryable = queryable.Where(doc => doc.EntityType == entityType);

        // Apply additional filter if provided
        if (filter != null)
        {
            queryable = queryable.Where(filter);
        }

        return queryable;
    }

    // ============================================
    // SAVE/UPDATE/DELETE OPERATIONS
    // ============================================

    public async Task<CosmosDbDocument<T>> Save<T>(
        string containerName,
        T entity,
        CosmosDbPartitionKey partitionKey
    )
        where T : ICosmosDbEntity
    {
        var container = GetContainer(containerName);
        var doc = CosmosDbDocument<T>.Create(entity, partitionKey.ToString());
        var response = await container.CreateItemAsync(doc, partitionKey.ToCosmosPartitionKey());
        return response.Resource;
    }

    public async Task<CosmosDbDocument<T>> Upsert<T>(
        string containerName,
        T entity,
        CosmosDbPartitionKey partitionKey
    )
        where T : ICosmosDbEntity
    {
        var container = GetContainer(containerName);
        var doc = CosmosDbDocument<T>.Create(entity, partitionKey.ToString());
        var response = await container.UpsertItemAsync(doc);
        return response.Resource;
    }

    public async Task Delete<T>(
        string containerName,
        string id,
        CosmosDbPartitionKey? partitionKey = null
    )
        where T : ICosmosDbEntity
    {
        var container = GetContainer(containerName);
        await container.DeleteItemAsync<T>(id, GetPartitionKey(partitionKey));
    }

    public async Task DeleteAll(CosmosDbContainerInfo containerInfo, CosmosDbPartitionKey partitionKey)
    {
        var container = GetContainer(containerInfo.Name);

        // Build SQL query to select id and all partition key paths
        var selectClause = CosmosDbDeleteHelper.BuildSelectClause(containerInfo.PartitionKeyPaths);
        var query = container.GetItemQueryIterator<dynamic>(
            new QueryDefinition(selectClause),
            requestOptions: new QueryRequestOptions
            {
                PartitionKey = partitionKey.ToCosmosPartitionKey()
            }
        );

        List<string> failedDeletes = new();
        int successCount = 0;
        int failureCount = 0;

        while (query.HasMoreResults)
        {
            var batch = await query.ReadNextAsync();
            var tasks = new List<Task<(string id, bool success, string? error)>>();

            foreach (var item in batch)
            {
                try
                {
                    var itemId = item.id.ToString();
                    
                    // Extract partition key values from the item and build the partition key
                    var itemPartitionKey = CosmosDbDeleteHelper.BuildPartitionKeyFromItem(item, containerInfo.PartitionKeyPaths);
                    
                    tasks.Add(CosmosDbDeleteHelper.DeleteItemSafeAsync(container, itemId, itemPartitionKey));

                    if (tasks.Count >= 10)
                    {
                        var results = await Task.WhenAll(tasks);
                        CosmosDbDeleteHelper.ProcessResults(results, ref successCount, ref failureCount, failedDeletes);
                        tasks.Clear();
                    }
                }
                catch (Exception ex)
                {
                    // If we can't extract partition key, record the failure
                    var itemId = item.id?.ToString() ?? "unknown";
                    failureCount++;
                    failedDeletes.Add($"{itemId} (Failed to extract partition key: {ex.Message})");
                }
            }

            if (tasks.Count > 0)
            {
                var results = await Task.WhenAll(tasks);
                CosmosDbDeleteHelper.ProcessResults(results, ref successCount, ref failureCount, failedDeletes);
            }
        }

        if (failedDeletes.Count > 0)
        {
            throw new AggregateException(
                $"DeleteAll partially failed. Success: {successCount}, Failed: {failureCount}. " +
                $"Failed IDs: {string.Join(", ", failedDeletes.Take(10))}{(failedDeletes.Count > 10 ? "..." : "")}",
                failedDeletes.Select(id => new Exception($"Failed to delete item: {id}"))
            );
        }
    }

    // ============================================
    // BATCH OPERATIONS
    // ============================================

    public void AddToBatch<T>(
        string containerName,
        CosmosDbPartitionKey partitionKey,
        params IEnumerable<T> items
    )
        where T : ICosmosDbEntity
    {
        if (string.IsNullOrWhiteSpace(partitionKey.GetKey()))
        {
            throw new PartitionKeyNullException();
        }

        if (items is null || !items.Any())
        {
            throw new ArgumentNullException(nameof(items));
        }

        if (!_containerBuffers.TryGetValue(containerName, out var buffer))
        {
            buffer = new ContainerBatchBuffer(GetContainer(containerName));
            _containerBuffers[containerName] = buffer;
        }

        var docs = items.Select(x => CosmosDbDocument<T>.Create(x, partitionKey.ToString()));
        var typedBuffer = (ContainerBatchBuffer)buffer;
        typedBuffer.AddDocument(partitionKey, docs); // Fixed: Pass partitionKey object, not string
    }

    public async Task<List<CosmosBatchResult>> SaveBatchAsync()
    {
        const int MAX_BATCH_SIZE = 100; // Cosmos DB limit for operations per transactional batch
        var batchResults = new List<CosmosBatchResult>();

        foreach (var bufferPair in _containerBuffers)
        {
            var containerName = bufferPair.Key;
            var buffer = bufferPair.Value;
            var container = buffer.Container;

            foreach (var partitionedItems in buffer.PartitionedItems)
            {
                var key = partitionedItems.Key;
                var items = partitionedItems.Value;

                if (items == null || items.Documents.Count == 0)
                {
                    continue;
                }

                // Chunk documents into groups of MAX_BATCH_SIZE to respect Cosmos DB limits
                var documentChunks = items.Documents
                    .Select((doc, index) => new { doc, index })
                    .GroupBy(x => x.index / MAX_BATCH_SIZE)
                    .Select(g => g.Select(x => x.doc).ToList())
                    .ToList();

                var totalDocuments = items.Documents.Count;
                var totalChunks = documentChunks.Count;

                // Process each chunk as a separate transactional batch
                for (int chunkIndex = 0; chunkIndex < documentChunks.Count; chunkIndex++)
                {
                    var chunk = documentChunks[chunkIndex];
                    var chunkNumber = chunkIndex + 1;
                    var itemRange = $"{chunkIndex * MAX_BATCH_SIZE + 1}-{chunkIndex * MAX_BATCH_SIZE + chunk.Count}";

                    var batch = container.CreateTransactionalBatch(
                        items.PartitionKey.ToCosmosPartitionKey()
                    );

                    foreach (var item in chunk)
                    {
                        batch.UpsertItem(item);
                    }

                    try
                    {
                        TransactionalBatchResponse response = await batch.ExecuteAsync();

                        if (response.IsSuccessStatusCode)
                        {
                            var message = totalChunks > 1
                                ? $"Successfully processed {chunk.Count} items (items {itemRange} of {totalDocuments}) in batch {chunkNumber}/{totalChunks}"
                                : null; // Don't clutter message if only one chunk

                            batchResults.Add(
                                CosmosBatchResult.Success(
                                    containerName,
                                    key,
                                    response.StatusCode,
                                    message
                                )
                            );
                        }
                        else
                        {
                            var message = totalChunks > 1
                                ? $"Batch {chunkNumber}/{totalChunks} (items {itemRange}): {response.ErrorMessage}"
                                : response.ErrorMessage;

                            batchResults.Add(
                                CosmosBatchResult.Failure(
                                    containerName,
                                    key,
                                    response.StatusCode,
                                    message
                                )
                            );
                        }
                    }
                    catch (CosmosException ex)
                    {
                        var message = totalChunks > 1
                            ? $"Batch {chunkNumber}/{totalChunks} (items {itemRange}): {ex.Message}"
                            : ex.Message;

                        batchResults.Add(
                            CosmosBatchResult.Failure(
                                containerName,
                                key,
                                ex.StatusCode,
                                message
                            )
                        );
                    }
                    catch (Exception ex)
                    {
                        var message = totalChunks > 1
                            ? $"Batch {chunkNumber}/{totalChunks} (items {itemRange}): {ex.Message}"
                            : ex.Message;

                        batchResults.Add(
                            CosmosBatchResult.Failure(containerName, key, null, message)
                        );
                    }
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
    )
        where T : ICosmosDbEntity
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

    public static async Task<PagedCosmosResult<T>> ToPagedResult<T>(
        this IQueryable<CosmosDbDocument<T>> query,
        int maxItemCount = 100,
        string? continuationToken = null,
        CancellationToken cancellationToken = default
    )
        where T : ICosmosDbEntity
    {
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

    public static async Task<PagedCosmosResult<T>> ToPagedResultWithSql<T>(
        this Container container,
        QueryDefinition queryDefinition,
        int maxItemCount = 100,
        string? continuationToken = null,
        CosmosDbPartitionKey? partitionKey = null,
        CancellationToken cancellationToken = default
    )
        where T : ICosmosDbEntity
    {
        var requestOptions = new QueryRequestOptions { MaxItemCount = maxItemCount };

        if (partitionKey != null)
        {
            requestOptions.PartitionKey = partitionKey.ToCosmosPartitionKey();
        }

        using var resultSetIterator = container.GetItemQueryIterator<CosmosDbDocument<T>>(
            queryDefinition,
            continuationToken,
            requestOptions
        );

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
