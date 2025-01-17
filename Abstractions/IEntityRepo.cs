namespace ClearDataService.Abstractions;

public interface IEntityRepo<T> where T : class
{
    Task<List<T>> Get();
    Task<T?> Get(int id);
    Task<T?> Get(string id);
    Task<T?> Get(Expression<Func<T, bool>> predicate);

    IQueryable<T> GetAsQueryable();

    Task<List<T>> Find(Expression<Func<T, bool>> predicate);

    int Count();
    int Count(Expression<Func<T, bool>> predicate);
    Task<bool> Exists(Expression<Func<T, bool>> predicate);
}