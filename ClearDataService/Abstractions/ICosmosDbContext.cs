using Clear.DataService.Abstractions;
using Clear.DataService.Entities.Cosmos;
using Clear.DataService.Models;
using Clear.DataService.Utils;

namespace Clear.DataService.Abstractions;

public interface ICosmosDbContext
{
    // ============================================
    // GET OPERATIONS - ID-based and filter-based separate
    // ============================================

    /// <summary>
    /// Gets a single entity by ID.
    /// Supports both string and hierarchical partition keys via implicit conversion.
    /// </summary>
    Task<T> Get<T>(
        string containerName,
        string id,
        CosmosDbPartitionKey? partitionKey = null,
        CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity;

    /// <summary>
    /// Gets a single entity using a filter predicate.
    /// Supports both string and hierarchical partition keys via implicit conversion.
    /// </summary>
    Task<T?> Get<T>(
        string containerName,
        FilterBuilder<T> filter,
        CosmosDbPartitionKey? partitionKey = null,
        CancellationToken cancellationToken = default
    ) where T : class, ICosmosDbEntity;

    /// <summary>
    /// Gets a list of entities with optional filtering and sorting.
    /// Supports both string and hierarchical partition keys via implicit conversion.
    /// </summary>
    Task<List<T>> GetList<T>(
        string containerName,
        CosmosDbPartitionKey? partitionKey = null,
        FilterBuilder<T>? filter = null,
        SortBuilder<T>? sortBuilder = null,
        CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity;

    // ============================================
    // DOCUMENT OPERATIONS - ID-based and filter-based separate
    // ============================================

    /// <summary>
    /// Gets a single document by ID.
    /// </summary>
    Task<CosmosDbDocument<T>> GetDocument<T>(
        string containerName,
        string id,
        CosmosDbPartitionKey? partitionKey = null,
        CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity;

    /// <summary>
    /// Gets a single document using a filter predicate.
    /// </summary>
    Task<CosmosDbDocument<T>?> GetDocument<T>(
        string containerName,
        FilterBuilder<T> filter,
        CosmosDbPartitionKey? partitionKey = null,
        CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity;

    /// <summary>
    /// Gets a list of documents with optional filtering and sorting.
    /// </summary>
    Task<List<CosmosDbDocument<T>>> GetDocuments<T>(
        string containerName,
        CosmosDbPartitionKey? partitionKey = null,
        FilterBuilder<T>? filter = null,
        SortBuilder<T>? sortBuilder = null,
        CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity;

    // ============================================
    // PAGINATION OPERATIONS - Consolidated with optional filter and sort
    // ============================================

    /// <summary>
    /// Gets a paged list of entities with optional filtering and sorting.
    /// </summary>
    Task<PagedResult<T>> GetPagedList<T>(
        string containerName,
        int pageSize = 100,
        string? continuationToken = null,
        CosmosDbPartitionKey? partitionKey = null,
        FilterBuilder<T>? filter = null,
        SortBuilder<T>? sortBuilder = null,
        CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity;

    /// <summary>
    /// Gets a paged list of documents with optional filtering and sorting.
    /// </summary>
    Task<PagedCosmosResult<T>> GetPagedDocuments<T>(
        string containerName,
        int pageSize = 100,
        string? continuationToken = null,
        CosmosDbPartitionKey? partitionKey = null,
        FilterBuilder<T>? filter = null,
        SortBuilder<T>? sortBuilder = null,
        CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity;

    // ============================================
    // SQL-BASED PAGINATION - Consolidated
    // ============================================

    /// <summary>
    /// Gets a paged list using SQL query with proper continuation token support.
    /// </summary>
    Task<PagedResult<T>> GetPagedListWithSql<T>(
        string containerName,
        string whereClause = "",
        Dictionary<string, object>? parameters = null,
        int pageSize = 100,
        string? continuationToken = null,
        CosmosDbPartitionKey? partitionKey = null,
        SortBuilder<T>? sortBuilder = null,
        CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity;

    /// <summary>
    /// Gets paged documents using SQL query with proper continuation token support.
    /// </summary>
    Task<PagedCosmosResult<T>> GetPagedDocumentsWithSql<T>(
        string containerName,
        string whereClause = "",
        Dictionary<string, object>? parameters = null,
        int pageSize = 100,
        string? continuationToken = null,
        CosmosDbPartitionKey? partitionKey = null,
        SortBuilder<T>? sortBuilder = null,
        CancellationToken cancellationToken = default
    ) where T : ICosmosDbEntity;

    // ============================================
    // QUERYABLE OPERATIONS - Consolidated
    // ============================================

    /// <summary>
    /// Exposes queryable for advanced scenarios with optional filtering.
    /// </summary>
    IQueryable<CosmosDbDocument<T>> GetAsQueryable<T>(
        string containerName,
        CosmosDbPartitionKey? partitionKey = null,
        FilterBuilder<T>? filter = null
    ) where T : ICosmosDbEntity;

    // ============================================
    // SAVE/UPDATE/DELETE OPERATIONS
    // ============================================

    /// <summary>
    /// Saves a new entity.
    /// Supports both string and hierarchical partition keys via implicit conversion.
    /// </summary>
    Task<CosmosDbDocument<T>> Save<T>(
        string containerName,
        T entity,
        CosmosDbPartitionKey partitionKey
    ) where T : ICosmosDbEntity;

    /// <summary>
    /// Upserts an entity (insert or update).
    /// Supports both string and hierarchical partition keys via implicit conversion.
    /// </summary>
    Task<CosmosDbDocument<T>> Upsert<T>(
        string containerName,
        T entity,
        CosmosDbPartitionKey partitionKey
    ) where T : ICosmosDbEntity;

    /// <summary>
    /// Deletes an entity by ID.
    /// Supports both string and hierarchical partition keys via implicit conversion.
    /// </summary>
    Task Delete<T>(
        string containerName,
        string id,
        CosmosDbPartitionKey? partitionKey = null
    ) where T : ICosmosDbEntity;

    // ============================================
    // BATCH OPERATIONS
    // ============================================

    /// <summary>
    /// Adds an entity to the batch buffer.
    /// Supports both string and hierarchical partition keys via implicit conversion.
    /// </summary>
    void AddToBatch<T>(
        string containerName,
        CosmosDbPartitionKey partitionKey,
        params IEnumerable<T> items
    ) where T : ICosmosDbEntity;

    /// <summary>
    /// Executes all batched operations.
    /// </summary>
    Task<List<CosmosBatchResult>> SaveBatchAsync();

    /// <summary>
    /// Deletes all entities within a specified partition.
    /// </summary>
    Task DeleteAll(CosmosDbContainerInfo containerName, CosmosDbPartitionKey partitionKey);
}