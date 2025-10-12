using Clear.DataService.Entities.Cosmos;
using Clear.DataService.Models;
using Clear.DataService.Utils;
using System.Linq.Expressions;

namespace Clear.DataService.Abstractions;

public interface ICosmosDbContext
{
    Task<T> Get<T>(
        string containerName,
        string id,
        string? partitionKey = null,
        CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity;

    Task<T?> Get<T>(
        string containerName,
        Func<T, bool> predicate,
        string? partitionKey = null,
        CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity;

    Task<List<T>> GetList<T>(
        string containerName,
        string? partitionKey = null,
        CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity;

    Task<List<T>> GetList<T>(
        string containerName,
        Func<T, bool> predicate,
        string? partitionKey = null,
        CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity;

    Task<CosmosDbDocument<T>> GetDocument<T>(
        string containerName,
        string id,
        string? partitionKey = null,
        CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity;

    Task<CosmosDbDocument<T>?> GetDocument<T>(
        string containerName,
        Func<CosmosDbDocument<T>, bool> predicate,
        string? partitionKey = null,
        CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity;

    Task<List<CosmosDbDocument<T>>> GetDocuments<T>(
        string containerName,
        string? partitionKey = null,
        CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity;

    Task<List<CosmosDbDocument<T>>> GetDocuments<T>(
        string containerName,
        Func<CosmosDbDocument<T>, bool> predicate,
        string? partitionKey = null,
        CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity;

    // Efficient pagination methods using continuation tokens
    Task<PagedResult<T>> GetPagedList<T>(
        string containerName,
        int pageSize = 100,
        string? continuationToken = null,
        string? partitionKey = null,
        SortBuilder<T>? sortBuilder = null,
        CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity;

    Task<PagedResult<T>> GetPagedList<T>(
        string containerName,
        Expression<Func<T, bool>> predicate,
        int pageSize = 100,
        string? continuationToken = null,
        string? partitionKey = null,
        SortBuilder<T>? sortBuilder = null,
        CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity;

    Task<PagedCosmosResult<T>> GetPagedDocuments<T>(
        string containerName,
        int pageSize = 100,
        string? continuationToken = null,
        string? partitionKey = null,
        SortBuilder<T>? sortBuilder = null,
        CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity;

    Task<PagedCosmosResult<T>> GetPagedDocuments<T>(
        string containerName,
        Expression<Func<CosmosDbDocument<T>, bool>> predicate,
        int pageSize = 100,
        string? continuationToken = null,
        string? partitionKey = null,
        SortBuilder<T>? sortBuilder = null,
        CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity;

    // SQL-based pagination for complex queries with better continuation token support
    Task<PagedResult<T>> GetPagedListWithSql<T>(
        string containerName,
        string whereClause = "",
        Dictionary<string, object>? parameters = null,
        int pageSize = 100,
        string? continuationToken = null,
        string? partitionKey = null,
        SortBuilder<T>? sortBuilder = null,
        CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity;

    Task<PagedCosmosResult<T>> GetPagedDocumentsWithSql<T>(
        string containerName,
        string whereClause = "",
        Dictionary<string, object>? parameters = null,
        int pageSize = 100,
        string? continuationToken = null,
        string? partitionKey = null,
        SortBuilder<T>? sortBuilder = null,
        CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity;

    // Expose queryable for advanced scenarios
    IQueryable<CosmosDbDocument<T>> GetAsQueryable<T>(
        string containerName,
        string? partitionKey = null
    ) where T : ICosmosDbEntity;

    IQueryable<CosmosDbDocument<T>> GetAsQueryable<T>(
        string containerName,
        Expression<Func<CosmosDbDocument<T>, bool>> predicate,
        string? partitionKey = null
    ) where T : ICosmosDbEntity;

    Task<CosmosDbDocument<T>> Save<T>(
        string containerName,
        T entity,
        string partitionKey
    ) where T : ICosmosDbEntity;

    Task<CosmosDbDocument<T>> Upsert<T>(
        string containerName,
        T entity,
        string partitionKey
    ) where T : ICosmosDbEntity;

    Task Delete<T>(
        string containerName,
        string id,
        string? partitionKey = null
    );

    // Hierarchical partition key support
    Task<T> Get<T>(
        string containerName,
        string id,
        HierarchicalPartitionKey hierarchicalPartitionKey,
        CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity;

    Task<List<T>> GetList<T>(
        string containerName,
        HierarchicalPartitionKey hierarchicalPartitionKey,
        CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity;

    Task<List<T>> GetList<T>(
        string containerName,
        Expression<Func<T, bool>> predicate,
        HierarchicalPartitionKey hierarchicalPartitionKey,
        CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity;

    Task<PagedResult<T>> GetPagedList<T>(
        string containerName,
        int pageSize,
        HierarchicalPartitionKey hierarchicalPartitionKey,
        string? continuationToken = null,
        SortBuilder<T>? sortBuilder = null,
        CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity;

    Task<PagedResult<T>> GetPagedList<T>(
        string containerName,
        Expression<Func<T, bool>> predicate,
        int pageSize,
        HierarchicalPartitionKey hierarchicalPartitionKey,
        string? continuationToken = null,
        SortBuilder<T>? sortBuilder = null,
        CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity;

    Task<CosmosDbDocument<T>> Save<T>(
        string containerName,
        T entity,
        HierarchicalPartitionKey hierarchicalPartitionKey
    ) where T : ICosmosDbEntity;

    Task<CosmosDbDocument<T>> Upsert<T>(
        string containerName,
        T entity,
        HierarchicalPartitionKey hierarchicalPartitionKey
    ) where T : ICosmosDbEntity;

    Task Delete<T>(
        string containerName,
        string id,
        HierarchicalPartitionKey hierarchicalPartitionKey
    );

    // Batch operations
    void AddToBatch<T>(
        string containerName,
        T item,
        string partitionKey
    ) where T : ICosmosDbEntity;

    Task<List<CosmosBatchResult>> SaveBatchAsync();
}