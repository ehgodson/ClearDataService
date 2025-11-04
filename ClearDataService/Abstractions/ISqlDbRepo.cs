
namespace Clear.DataService.Abstractions;

public interface ISqlDbRepo<T> where T : class
{
    int Count();
    int Count(Expression<Func<T, bool>> predicate);
    Task Create(T entity);
    Task Create(IEnumerable<T> entities);
    Task Delete(T entity);
    Task Delete(IEnumerable<T> entities);
    Task<bool> Exists(Expression<Func<T, bool>> predicate);
    Task<List<T>> Find(Expression<Func<T, bool>> predicate);
    Task<List<T>> Get();
    Task<T?> Get(int id);
    Task<T?> Get(string id);
    Task<T?> Get(Expression<Func<T, bool>> predicate);
    IQueryable<T> GetAsQueryable();
    Task Update(T entity);
    Task Update(IEnumerable<T> entities);
}