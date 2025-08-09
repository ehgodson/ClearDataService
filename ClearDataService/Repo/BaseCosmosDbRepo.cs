using ClearDataService.Abstractions;
using ClearDataService.Entities.Cosmos;

namespace ClearDataService.Repo;

public abstract class BaseCosmosDbRepo<T> : ICosmosDbRepo<T> where T : ICosmosDbEntity
{
    private readonly ICosmosDbContext _context;
    private readonly string _containerName;

    protected BaseCosmosDbRepo(ICosmosDbContext context, string containerName)
    {
        _context = context;
        _containerName = containerName;
    }

    public async Task<T> Get(string id, string? partitionKey)
    => await _context.Get<T>(_containerName, id, partitionKey);

    public async Task<List<T>> Get(string? partitionKey)
    => await _context.GetList<T>(_containerName, partitionKey);

    public async Task<List<T>> Get(Func<T, bool> predicate, string? partitionKey)
    => await _context.GetList(_containerName, predicate, partitionKey);

    public async Task<T> Create(T entity, string partitionKey)
    => GetData(await _context.Save<T>(_containerName, entity, partitionKey));

    public async Task<T> Update(T entity, string partitionKey)
    => GetData(await _context.Upsert<T>(_containerName, entity, partitionKey));

    public async Task Delete(string id, string partitionKey)
    => await _context.Delete<T>(_containerName, id, partitionKey);

    private static T GetData(CosmosDbDocument<T> document)
    {
        return document.Data;
    }
}