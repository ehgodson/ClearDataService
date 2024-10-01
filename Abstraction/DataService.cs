using Dapper;
using Microsoft.EntityFrameworkCore;
using System.Data.SqlClient;

namespace ClearDataService.Abstraction;

public abstract class AbstractDataService(DbContext db) : IDataService
{
    private readonly string _connectionString = db.Database.GetConnectionString() ?? "";

    #region ef methods

    #region Get Data

    public DbSet<T> GetEntity<T>() where T : class
    {
        return db.Set<T>();
    }

    public async Task<T?> Get<T>(int id) where T : class
    {
        return await db.Set<T>().FindAsync(id);
    }

    public async Task<T?> Get<T>(string id) where T : class
    {
        return await db.Set<T>().FindAsync(id);
    }

    public async Task<List<T>> Get<T>(bool trackEntities = false) where T : class
    {
        var entity = trackEntities ? db.Set<T>() : db.Set<T>().AsNoTracking();
        return await entity.ToListAsync();
    }

    public async Task<List<T>> Find<T>(Expression<Func<T, bool>> predicate, bool trackEntities = false) where T : class
    {
        var entity = trackEntities ? db.Set<T>() : db.Set<T>().AsNoTracking();
        return await entity.Where(predicate).ToListAsync();
    }

    public IQueryable<T> GetAsQueryable<T>() where T : class
    {
        return db.Set<T>().AsNoTracking();
    }

    public IQueryable<T> FindAsQueryable<T>(Expression<Func<T, bool>> predicate) where T : class
    {
        return db.Set<T>().AsNoTracking().Where(predicate);
    }

    public async Task<T?> Get<T>(Expression<Func<T, bool>> predicate) where T : class
    {
        return await db.Set<T>().FirstOrDefaultAsync(predicate);
    }

    public int Count<T>() where T : class
    {
        return db.Set<T>().Count();
    }

    public int Count<T>(Expression<Func<T, bool>> predicate) where T : class
    {
        return db.Set<T>().Count(predicate);
    }

    public async Task<bool> Exists<T>(Expression<Func<T, bool>> predicate) where T : class
    {
        return await db.Set<T>().AnyAsync(predicate);
    }

    #endregion

    #region Instant Update

    public async Task<T> Save<T>(T entity) where T : class
    {
        var ob = db.Add(entity);
        await db.SaveChangesAsync();
        return ob.Entity;
    }

    public async Task<int> Save<T>(IEnumerable<T> entities) where T : class
    {
        db.AddRange(entities);
        return await db.SaveChangesAsync();
    }

    public async Task<T> Update<T>(T entity) where T : class
    {
        var ob = db.Update(entity);
        await db.SaveChangesAsync();
        return ob.Entity;
    }

    public async Task<int> Update<T>(IEnumerable<T> entities) where T : class
    {
        entities.ToList().ForEach(entity => db.Update(entity));
        return await db.SaveChangesAsync();
    }

    public async Task<int> Delete<T>(T entity) where T : class
    {
        db.Remove(entity);
        return await db.SaveChangesAsync();
    }

    public async Task<int> Delete<T>(IEnumerable<T> entities) where T : class
    {
        entities.ToList().ForEach(entity => db.Remove(entity));
        return await db.SaveChangesAsync();
    }

    #endregion

    #region Pre Update

    public void AddForInsert<T>(T entity) where T : class => db.Add(entity);

    public void AddAllForInsert<T>(IEnumerable<T> entities) where T : class => db.AddRange(entities);

    public void AddForUpdate<T>(T entity) where T : class => db.Update(entity);

    public void AddAllForUpdate<T>(IEnumerable<T> entities) where T : class =>
        entities.ToList().ForEach(entity => db.Update(entity));

    public void AddForDelete<T>(T entity) where T : class => db.Remove(entity);

    public void AddAllForDelete<T>(IEnumerable<T> entities) where T : class =>
        entities.ToList().ForEach(entity => db.Remove(entity));

    public async Task<int> SaveChanges() => await db.SaveChangesAsync();

    #endregion

    #region EF querying

    public async Task<List<T>> FromSql<T>(string sql) where T : class =>
        await db.Set<T>().FromSqlRaw(sql).ToListAsync();

    public async Task<int> ExecuteSql(string sql) =>
        await db.Database.ExecuteSqlRawAsync(sql);

    public async Task<int> ExecuteSql(string sql, params object[] parameters) =>
        await db.Database.ExecuteSqlRawAsync(sql, parameters);

    #endregion

    #endregion

    #region dappar methods

    public async Task<List<T>> Query<T>(string sql)
    {
        using SqlConnection conn = new(_connectionString);
        return (await conn.QueryAsync<T>(sql)).ToList();
    }

    public async Task<List<T>> Query<T>(string sql, object parameters)
    {
        using SqlConnection conn = new(_connectionString);
        return (await conn.QueryAsync<T>(sql, parameters)).ToList();
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