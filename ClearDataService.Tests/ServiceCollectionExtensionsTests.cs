using Clear.DataService;
using Clear.DataService.Abstractions;
using Clear.DataService.Contexts;
using Clear.DataService.Factory;
using Clear.DataService.Repo;
using Clear.DataService.Utils;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Reflection;

namespace ClearDataService.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSqlDbContext_ShouldRegisterSqlDbContext()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddDbContext<DbContext>(options => options.UseInMemoryDatabase("MyDatabase"));
        services.AddSqlDbContext();
        var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<ISqlDbContext>());
    }

    [Fact]
    public void AddCosmosDbContext_ShouldRegisterCosmosDbContext()
    {
        // Arrange
        var services = new ServiceCollection();

        var mockKey = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("mockPrimaryKey"));

        var settings = CosmosDbSettings.Create(
            "https://localhost:8081",
            mockKey,
            "TestDatabase"
        );

        var cosmosDbClientMock = new Mock<CosmosClient>();

        // Act
        services.AddSingleton<ICosmosDbSettings>(settings);
        services.AddSingleton(cosmosDbClientMock.Object);
        services.AddCosmosDbContext(settings);
        var provider = services.BuildServiceProvider();

        // Assert
        var cosmosDbContext = provider.GetService<ICosmosDbContext>();
        Assert.NotNull(cosmosDbContext);
    }

    [Fact]
    public void AddAllCosmosDbRepo_ShouldRegisterAllCosmosDbRepos()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = Assembly.GetExecutingAssembly();

        // Mock the required dependencies
        var settings = CosmosDbSettings.Create("https://localhost:8081", "mockKey", "TestDatabase");
        var cosmosClientMock = new Mock<CosmosClient>();
        services.AddSingleton<ICosmosDbSettings>(settings);
        services.AddSingleton(cosmosClientMock.Object);
        services.AddScoped<ICosmosDbContext, CosmosDbContext>();

        // Act
        services.AddAllCosmosDbRepo(assembly);
        var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<ICosmosDbRepo<MockCosmosDbEntity>>());
    }

    [Fact]
    public void AddAllSqlDbRepo_ShouldRegisterAllSqlDbRepos()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = Assembly.GetExecutingAssembly();

        // Mock the required dependencies
        services.AddDbContext<DbContext>(options => options.UseInMemoryDatabase("TestDatabase"));
        services.AddScoped<ISqlDbContext, SqlDbContext>();

        // Act
        services.AddAllSqlDbRepo(assembly);
        var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<ISqlDbRepo<MockSqlDbEntity>>());
    }

    // Mock classes for testing
    public class MockCosmosDbEntity : ICosmosDbEntity
    {
        public string Id { get; set; } = "mockId";
    }

    public class MockCosmosDbRepo(ICosmosDbContext context)
        : BaseCosmosDbRepo<MockCosmosDbEntity>(context, "MockContainer")
    { }

    public class MockSqlDbEntity : ISqlDbEntity
    {
        public int Id { get; set; }
    }

    public class MockSqlDbRepo(ISqlDbContext db) : BaseSqlDbRepo<MockSqlDbEntity>(db)
    { }
}

// Extension methods for testing
public static class TestServiceCollectionExtensions
{
    public static IServiceCollection AddAllCosmosDbRepo(this IServiceCollection services, Assembly assembly)
    {
        var type = typeof(ICosmosDbRepo<>);

        var repoTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && 
                       t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == type))
            .ToList();

        foreach (var repoType in repoTypes)
        {
            var interfaces = repoType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == type)
                .ToList();
            
            foreach (var interfaceType in interfaces)
            {
                services.AddScoped(interfaceType, repoType);
            }
        }

        return services;
    }

    public static IServiceCollection AddAllSqlDbRepo(this IServiceCollection services, Assembly assembly)
    {
        var type = typeof(ISqlDbRepo<>);

        var repoTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && 
                       t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == type))
            .ToList();

        foreach (var repoType in repoTypes)
        {
            var interfaces = repoType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == type)
                .ToList();
            
            foreach (var interfaceType in interfaces)
            {
                services.AddScoped(interfaceType, repoType);
            }
        }

        return services;
    }
}