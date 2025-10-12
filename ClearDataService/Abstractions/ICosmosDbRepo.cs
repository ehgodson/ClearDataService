using Clear.DataService.Entities.Cosmos;

namespace Clear.DataService.Abstractions;

public interface ICosmosDbRepo<T> where T : ICosmosDbEntity
{
    Task<T> Get(string id, string? partitionKey);
    Task<List<T>> Get(string? partitionKey);
    Task<List<T>> Get(Func<T, bool> predicate, string? partitionKey);
    Task<T> Create(T entity, string partitionKey);
    Task<T> Update(T entity, string partitionKey);
    Task Delete(string id, string partitionKey);
}