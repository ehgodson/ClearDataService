using ClearDataService.Abstractions;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;

namespace ClearDataService.Services;

/// <summary>
/// This class is the base class for all data services in the project.
/// Every method is async and returns a Task except ones returning, Entity, IQueryable or Count.
/// </summary>
/// <param name="db"></param>
public abstract class BaseDataService(DbContext db) : IDataService
{
    private readonly string _connectionString = db.Database.GetConnectionString() ?? "";

    #region ef methods

    #region Get Data

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

    public async Task<T?> Get<T>(Expression<Func<T, bool>> predicate, bool trackEntities = true) where T : class
    {
        var entity = trackEntities ? db.Set<T>() : db.Set<T>().AsNoTracking();
        return await entity.FirstOrDefaultAsync(predicate);
    }

    public async Task<T?> GetOne<T>(bool trackEntities = false) where T : class
    {
        var entity = trackEntities ? db.Set<T>() : db.Set<T>().AsNoTracking();
        return await entity.FirstOrDefaultAsync();
    }

    public async Task<List<T>> Find<T>(Expression<Func<T, bool>> predicate, bool trackEntities = false) where T : class
    {
        var entity = trackEntities ? db.Set<T>() : db.Set<T>().AsNoTracking();
        return await entity.Where(predicate).ToListAsync();
    }


    public DbSet<T> GetEntity<T>() where T : class
    {
        return db.Set<T>();
    }

    public IQueryable<T> GetAsQueryable<T>(bool trackEntities = false) where T : class
    {
        var entity = trackEntities ? db.Set<T>() : db.Set<T>().AsNoTracking();
        return entity.AsQueryable();
    }

    public IQueryable<T> FindAsQueryable<T>(Expression<Func<T, bool>> predicate, bool trackEntities = false) where T : class
    {
        var entity = trackEntities ? db.Set<T>() : db.Set<T>().AsNoTracking();
        return entity.AsQueryable().Where(predicate);
    }


    public int Count<T>() where T : class
    {
        return db.Set<T>().AsNoTracking().Count();
    }

    public int Count<T>(Expression<Func<T, bool>> predicate) where T : class
    {
        return db.Set<T>().AsNoTracking().Count(predicate);
    }

    public async Task<bool> Exists<T>(Expression<Func<T, bool>> predicate) where T : class
    {
        return await db.Set<T>().AsNoTracking().AnyAsync(predicate);
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
        return [.. (await conn.QueryAsync<T>(sql))];
    }

    public async Task<List<T>> Query<T>(string sql, object parameters)
    {
        using SqlConnection conn = new(_connectionString);
        return [.. (await conn.QueryAsync<T>(sql, parameters))];
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

public class DataService(DbContext db) : BaseDataService(db)
{ }

public static class DataServiceMiddlewareExtension
{
    /// <summary>
    /// This method adds the DataService to the service collection, which can be used to interact with the database.
    /// This is useful when you do no need to extend BaseDataService further in your project.
    /// If you have a custom DbContext, you can use the services.AddDbContext<DbContext, CustomDbContext> instead.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddDataService(this IServiceCollection services)
    {
        services.AddScoped<IDataService, DataService>();
        return services;
    }
}