namespace ClearDataService.Abstraction;

public abstract class AbstractGenericService<T>(IDataService db) : IGenericService<T> where T : class
{
    public async Task<List<T>> Get() => await db.Get<T>();
    public IQueryable<T> GetAsQueryable() => db.GetAsQueryable<T>();

    public async Task<T?> Get(int id) => await db.Get<T>(id);
    public async Task<T?> Get(string id) => await db.Get<T>(id);
    public async Task<T?> Get(Expression<Func<T, bool>> predicate) => await db.Get(predicate);

    public async Task<List<T>> Find(Expression<Func<T, bool>> predicate) => await db.Find(predicate);

    public int Count() => db.Count<T>();
    public int Count(Expression<Func<T, bool>> predicate) => db.Count(predicate);
    public async Task<bool> Exists(Expression<Func<T, bool>> predicate) => await db.Exists(predicate);
}