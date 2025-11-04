namespace Clear.DataService.Abstractions;

public interface ISqlDbContext
{
    Task<T?> Get<T>(int id) where T : class;
    Task<T?> Get<T>(string id) where T : class;
    Task<List<T>> Get<T>(bool trackEntities = false) where T : class;
    Task<T?> Get<T>(Expression<Func<T, bool>> predicate, bool trackEntities = true) where T : class;
    Task<T?> GetOne<T>(bool trackEntities = false) where T : class;
    Task<List<T>> Find<T>(Expression<Func<T, bool>> predicate, bool trackEntities = false) where T : class;

    DbSet<T> GetEntity<T>() where T : class;
    IQueryable<T> GetAsQueryable<T>(bool trackEntities = false) where T : class;
    IQueryable<T> FindAsQueryable<T>(Expression<Func<T, bool>> predicate, bool trackEntities = false) where T : class;


    int Count<T>() where T : class;
    int Count<T>(Expression<Func<T, bool>> predicate) where T : class;
    Task<bool> Exists<T>(Expression<Func<T, bool>> predicate) where T : class;


    Task<T> Save<T>(T entity) where T : class;
    Task<int> Save<T>(IEnumerable<T> entities) where T : class;
    Task<T> Update<T>(T entity) where T : class;
    Task<int> Update<T>(IEnumerable<T> entities) where T : class;
    Task<int> Delete<T>(T entity) where T : class;
    Task<int> Delete<T>(Expression<Func<T, bool>> predicate) where T : class;
    Task<int> Delete<T>(IEnumerable<T> entities) where T : class;


    void AddForInsert<T>(T entity) where T : class;
    void AddAllForInsert<T>(IEnumerable<T> entities) where T : class;
    void AddForDelete<T>(T entity) where T : class;
    void AddAllForDelete<T>(IEnumerable<T> entities) where T : class;
    void AddForUpdate<T>(T entity) where T : class;
    void AddAllForUpdate<T>(IEnumerable<T> entities) where T : class;
    Task<int> SaveChanges();


    //Task<List<T>> FromSql<T>(string sql) where T : class;
    Task<int> ExecuteSql(string sql);
    Task<int> ExecuteSql(string sql, params object[] parameters);


    Task<List<T>> Query<T>(string sql);
    Task<List<T>> Query<T>(string sql, object parameters);
    Task<T?> QueryFirstOrDefault<T>(string sql);
    Task<T?> QueryFirstOrDefault<T>(string sql, object parameters);
    //Task<int> ExecuteQuery(string sql);
}