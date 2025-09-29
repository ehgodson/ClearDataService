using ClearDataService.Entities.Cosmos;
using ClearDataService.Models;

namespace ClearDataService.Abstractions;

public interface ICosmosDbContext
{
    Task<T> Get<T>(string containerName, string id, string? partitionKey = null, CancellationToken cancellationToken = default) where T : ICosmosDbEntity;
    Task<T?> Get<T>(string containerName, Func<T, bool> predicate, string? partitionKey = null, CancellationToken cancellationToken = default) where T : ICosmosDbEntity;
    //IQueryable<T> GetAsQueryable<T>(string containerName, Expression<Func<CosmosDbDocument<T>, bool>> predicate, string? partitionKey = null) where T : ICosmosDbEntity;
    //IQueryable<T> GetAsQueryable<T>(string containerName, string? partitionKey = null) where T : ICosmosDbEntity;
    Task<List<T>> GetList<T>(string containerName, string? partitionKey = null, CancellationToken cancellationToken = default) where T : ICosmosDbEntity;
    Task<List<T>> GetList<T>(string containerName, Func<T, bool> predicate, string? partitionKey = null, CancellationToken cancellationToken = default) where T : ICosmosDbEntity;

    Task<CosmosDbDocument<T>> GetDocument<T>(string containerName, string id, string? partitionKey = null, CancellationToken cancellationToken = default) where T : ICosmosDbEntity;
    Task<CosmosDbDocument<T>?> GetDocument<T>(string containerName, Func<CosmosDbDocument<T>, bool> predicate, string? partitionKey = null, CancellationToken cancellationToken = default) where T : ICosmosDbEntity;
    //IQueryable<CosmosDbDocument<T>> GetDocumentsAsQueryable<T>(string containerName, Expression<Func<CosmosDbDocument<T>, bool>> predicate, string? partitionKey = null) where T : ICosmosDbEntity;
    //IQueryable<CosmosDbDocument<T>> GetDocumentsAsQueryable<T>(string containerName, string? partitionKey = null) where T : ICosmosDbEntity;
    Task<List<CosmosDbDocument<T>>> GetDocuments<T>(string containerName, string? partitionKey = null, CancellationToken cancellationToken = default) where T : ICosmosDbEntity;
    Task<List<CosmosDbDocument<T>>> GetDocuments<T>(string containerName, Func<CosmosDbDocument<T>, bool> predicate, string? partitionKey = null, CancellationToken cancellationToken = default) where T : ICosmosDbEntity;

    Task<CosmosDbDocument<T>> Save<T>(string containerName, T entity, string partitionKey) where T : ICosmosDbEntity;
    Task<CosmosDbDocument<T>> Upsert<T>(string containerName, T entity, string partitionKey) where T : ICosmosDbEntity;
    Task Delete<T>(string containerName, string id, string? partitionKey = null);
    void AddToBatch<T>(string containerName, T item, string partitionKey) where T : ICosmosDbEntity;
    Task<List<CosmosBatchResult>> SaveBatchAsync();
}