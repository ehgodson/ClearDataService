using ClearDataService.Abstractions;

namespace ClearDataService.Repo;

public abstract class BaseSqlDbRepo<T>(ISqlDbContext db) : ISqlDbRepo<T> where T : class
{
    public async Task<List<T>> Get() => await db.Get<T>();
    public async Task<T?> Get(int id) => await db.Get<T>(id);
    public async Task<T?> Get(string id) => await db.Get<T>(id);
    public async Task<T?> Get(Expression<Func<T, bool>> predicate) => await db.Get(predicate);

    public IQueryable<T> GetAsQueryable() => db.GetAsQueryable<T>();

    public async Task<List<T>> Find(Expression<Func<T, bool>> predicate) => await db.Find(predicate);

    public int Count() => db.Count<T>();
    public int Count(Expression<Func<T, bool>> predicate) => db.Count(predicate);
    public async Task<bool> Exists(Expression<Func<T, bool>> predicate) => await db.Exists(predicate);

    public async Task Create(T entity) => await db.Save(entity);
    public async Task Create(IEnumerable<T> entities) => await db.Save(entities);
    public async Task Update(T entity) => await db.Update(entity);
    public async Task Update(IEnumerable<T> entities) => await db.Update(entities);
    public async Task Delete(T entity) => await db.Delete(entity);
    public async Task Delete(IEnumerable<T> entities) => await db.Delete(entities);
}