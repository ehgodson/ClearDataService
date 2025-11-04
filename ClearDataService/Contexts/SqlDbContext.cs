using Clear.DataService.Abstractions;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Clear.DataService.Contexts;

/// <summary>
/// This class is the base class for all data services in the project.
/// Every method is async and returns a Task except ones returning, Entity, IQueryable or Count.
/// </summary>
/// <param name="_context"></param>
public abstract class BaseSqlDbContext(DbContext _context) : ISqlDbContext
{
    private readonly string _connectionString = _context.Database.IsRelational()
        ? _context.Database.GetConnectionString() ?? "" : "";

    #region ef methods

    #region Get Data

    public async Task<T?> Get<T>(int id) where T : class
    {
        return await _context.Set<T>().FindAsync(id);
    }

    public async Task<T?> Get<T>(string id) where T : class
    {
        return await _context.Set<T>().FindAsync(id);
    }

    public async Task<List<T>> Get<T>(bool trackEntities = false) where T : class
    {
        var entity = trackEntities ? _context.Set<T>() : _context.Set<T>().AsNoTracking();
        return await entity.ToListAsync();
    }

    public async Task<T?> Get<T>(Expression<Func<T, bool>> predicate, bool trackEntities = true) where T : class
    {
        var entity = trackEntities ? _context.Set<T>() : _context.Set<T>().AsNoTracking();
        return await entity.FirstOrDefaultAsync(predicate);
    }

    public async Task<T?> GetOne<T>(bool trackEntities = false) where T : class
    {
        var entity = trackEntities ? _context.Set<T>() : _context.Set<T>().AsNoTracking();
        return await entity.FirstOrDefaultAsync();
    }

    public async Task<List<T>> Find<T>(Expression<Func<T, bool>> predicate, bool trackEntities = false) where T : class
    {
        var entity = trackEntities ? _context.Set<T>() : _context.Set<T>().AsNoTracking();
        return await entity.Where(predicate).ToListAsync();
    }


    public DbSet<T> GetEntity<T>() where T : class
    {
        return _context.Set<T>();
    }

    public IQueryable<T> GetAsQueryable<T>(bool trackEntities = false) where T : class
    {
        var entity = trackEntities ? _context.Set<T>() : _context.Set<T>().AsNoTracking();
        return entity.AsQueryable();
    }

    public IQueryable<T> FindAsQueryable<T>(Expression<Func<T, bool>> predicate, bool trackEntities = false) where T : class
    {
        var entity = trackEntities ? _context.Set<T>() : _context.Set<T>().AsNoTracking();
        return entity.AsQueryable().Where(predicate);
    }


    public int Count<T>() where T : class
    {
        return _context.Set<T>().AsNoTracking().Count();
    }

    public int Count<T>(Expression<Func<T, bool>> predicate) where T : class
    {
        return _context.Set<T>().AsNoTracking().Count(predicate);
    }

    public async Task<bool> Exists<T>(Expression<Func<T, bool>> predicate) where T : class
    {
        return await _context.Set<T>().AsNoTracking().AnyAsync(predicate);
    }

    #endregion

    #region Instant Update

    public async Task<T> Save<T>(T entity) where T : class
    {
        var ob = _context.Add(entity);
        await _context.SaveChangesAsync();
        return ob.Entity;
    }

    public async Task<int> Save<T>(IEnumerable<T> entities) where T : class
    {
        _context.AddRange(entities);
        return await _context.SaveChangesAsync();
    }

    public async Task<T> Update<T>(T entity) where T : class
    {
        var ob = _context.Update(entity);
        await _context.SaveChangesAsync();
        return ob.Entity;
    }

    public async Task<int> Update<T>(IEnumerable<T> entities) where T : class
    {
        _context.UpdateRange(entities);
        return await _context.SaveChangesAsync();
    }

    public async Task<int> Delete<T>(T entity) where T : class
    {
        _context.Remove(entity);
        return await _context.SaveChangesAsync();
    }

    public async Task<int> Delete<T>(Expression<Func<T, bool>> predicate) where T : class
    {
        return await _context.Set<T>().Where(predicate).ExecuteDeleteAsync();
    }

    public async Task<int> Delete<T>(IEnumerable<T> entities) where T : class
    {
        _context.RemoveRange(entities);
        return await _context.SaveChangesAsync();
    }

    #endregion

    #region Pre Update

    public void AddForInsert<T>(T entity) where T : class => _context.Add(entity);

    public void AddAllForInsert<T>(IEnumerable<T> entities) where T : class => _context.AddRange(entities);

    public void AddForUpdate<T>(T entity) where T : class => _context.Update(entity);

    public void AddAllForUpdate<T>(IEnumerable<T> entities) where T : class =>
        entities.ToList().ForEach(entity => _context.Update(entity));

    public void AddForDelete<T>(T entity) where T : class => _context.Remove(entity);

    public void AddAllForDelete<T>(IEnumerable<T> entities) where T : class =>
        entities.ToList().ForEach(entity => _context.Remove(entity));

    public async Task<int> SaveChanges() => await _context.SaveChangesAsync();

    #endregion

    #region EF querying

    public async Task<List<T>> FromSql<T>(string sql) where T : class =>
        await _context.Set<T>().FromSqlRaw(sql).ToListAsync();

    public async Task<int> ExecuteSql(string sql) =>
        await _context.Database.ExecuteSqlRawAsync(sql);

    public async Task<int> ExecuteSql(string sql, params object[] parameters) =>
        await _context.Database.ExecuteSqlRawAsync(sql, parameters);

    #endregion

    #endregion

    #region dappar methods

    public async Task<List<T>> Query<T>(string sql)
    {
        using SqlConnection conn = new(_connectionString);
        return [.. await conn.QueryAsync<T>(sql)];
    }

    public async Task<List<T>> Query<T>(string sql, object parameters)
    {
        using SqlConnection conn = new(_connectionString);
        return [.. await conn.QueryAsync<T>(sql, parameters)];
    }

    public async Task<T?> QueryFirstOrDefault<T>(string sql)
    {
        using SqlConnection conn = new(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<T>(sql);
    }

    public async Task<T?> QueryFirstOrDefault<T>(string sql, object parameters)
    {
        using SqlConnection conn = new(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<T>(sql, parameters);
    }

    public async Task<int> ExecuteQuery(string sql)
    {
        using SqlConnection conn = new(_connectionString);
        return await conn.ExecuteAsync(sql);
    }

    #endregion
}

public class SqlDbContext(DbContext db) : BaseSqlDbContext(db)
{ }