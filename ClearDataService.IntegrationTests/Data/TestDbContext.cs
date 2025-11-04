using ClearDataService.IntegrationTests.TestEntities;
using Microsoft.EntityFrameworkCore;

namespace ClearDataService.IntegrationTests.Data;

/// <summary>
/// Test DbContext for SQL Server integration tests
/// </summary>
public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }

    public DbSet<SqlProduct> Products => Set<SqlProduct>();
    public DbSet<SqlOrder> Orders => Set<SqlOrder>();
    public DbSet<SqlOrderItem> OrderItems => Set<SqlOrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure relationships and constraints
        modelBuilder.Entity<SqlOrderItem>()
          .HasOne(oi => oi.Order)
            .WithMany(o => o.Items)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
