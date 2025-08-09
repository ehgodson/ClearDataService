namespace ClearDataService.Migrators;

public interface ISqlDbMigrator
{
    Task MigrateDatabase();
}

public class SqlDbMigrator : ISqlDbMigrator
{
    private readonly DbContext _context;

    public SqlDbMigrator(DbContext context)
    {
        _context = context;
    }

    public async Task MigrateDatabase()
    {
        await _context.Database.MigrateAsync();
    }
}