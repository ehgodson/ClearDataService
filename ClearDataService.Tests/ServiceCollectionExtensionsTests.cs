using Clear.DataService;
using Clear.DataService.Abstractions;
using Clear.DataService.Contexts;
using Clear.DataService.Factory;
using Clear.DataService.Utils;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

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

}
