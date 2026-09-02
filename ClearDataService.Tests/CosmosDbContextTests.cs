using Clear.DataService;
using Clear.DataService.Abstractions;
using Clear.DataService.Contexts;
using Clear.DataService.Entities.Cosmos;
using Clear.DataService.Models;
using Clear.DataService.Utils;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace ClearDataService.Tests;

public class CosmosDbContextTests
{
    private readonly Mock<CosmosClient> _client = new();
    private readonly Mock<Container> _container = new();
    private readonly Mock<ICosmosDbSettings> _settings = new();

    public CosmosDbContextTests()
    {
        _settings.SetupGet(x => x.DatabaseName).Returns("database");
        _client.Setup(x => x.GetContainer("database", "items")).Returns(_container.Object);
    }

    [Fact]
    public async Task GetById_UsesPointRead()
    {
        var entity = new TestEntity { Id = "entity-1", Name = "Expected" };
        var document = CosmosDbDocument<TestEntity>.Create(entity, "business-1");
        var response = new Mock<ItemResponse<CosmosDbDocument<TestEntity>>>();
        response.SetupGet(x => x.Resource).Returns(document);
        _container.Setup(x => x.ReadItemAsync<CosmosDbDocument<TestEntity>>(
                "entity-1",
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response.Object);

        var context = new CosmosDbContext(_client.Object, _settings.Object);

        var result = await context.Get<TestEntity>("items", "entity-1", "business-1");

        Assert.Same(entity, result);
        _container.Verify(x => x.ReadItemAsync<CosmosDbDocument<TestEntity>>(
            "entity-1",
            It.IsAny<PartitionKey>(),
            It.IsAny<ItemRequestOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReplaceIfMatch_SendsEtagAndPartitionKey()
    {
        var entity = new TestEntity { Id = "entity-1", Name = "Updated" };
        var responseDocument = CosmosDbDocument<TestEntity>.Create(entity, "business-1");
        var response = new Mock<ItemResponse<CosmosDbDocument<TestEntity>>>();
        response.SetupGet(x => x.Resource).Returns(responseDocument);
        ItemRequestOptions? capturedOptions = null;
        _container.Setup(x => x.ReplaceItemAsync(
                It.IsAny<CosmosDbDocument<TestEntity>>(),
                "entity-1",
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<CosmosDbDocument<TestEntity>, string, PartitionKey?, ItemRequestOptions, CancellationToken>(
                (_, _, _, options, _) => capturedOptions = options)
            .ReturnsAsync(response.Object);

        var context = new CosmosDbContext(_client.Object, _settings.Object);

        var result = await context.ReplaceIfMatch(
            "items", entity, "business-1", "etag-1", CancellationToken.None);

        Assert.Same(responseDocument, result);
        Assert.Equal("etag-1", capturedOptions?.IfMatchEtag);
    }

    [Fact]
    public async Task DeleteIfMatch_SendsEtagAndPartitionKey()
    {
        ItemRequestOptions? capturedOptions = null;
        _container.Setup(x => x.DeleteItemAsync<CosmosDbDocument<TestEntity>>(
                "entity-1",
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, PartitionKey, ItemRequestOptions, CancellationToken>(
                (_, _, options, _) => capturedOptions = options)
            .ReturnsAsync(Mock.Of<ItemResponse<CosmosDbDocument<TestEntity>>>());

        var context = new CosmosDbContext(_client.Object, _settings.Object);

        await context.DeleteIfMatch<TestEntity>(
            "items", "entity-1", "business-1", "etag-1", CancellationToken.None);

        Assert.Equal("etag-1", capturedOptions?.IfMatchEtag);
    }

    [Fact]
    public void GetAsQueryable_AppliesPageSizeAndContinuationToken()
    {
        QueryRequestOptions? capturedOptions = null;
        string? capturedContinuationToken = null;

        _container.Setup(x => x.GetItemLinqQueryable<CosmosDbDocument<TestEntity>>(
                false,
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>(),
                It.IsAny<CosmosLinqSerializerOptions>()))
            .Callback<bool, string, QueryRequestOptions, CosmosLinqSerializerOptions>(
                (_, token, options, _) =>
                {
                    capturedContinuationToken = token;
                    capturedOptions = options;
                })
            .Returns(Array.Empty<CosmosDbDocument<TestEntity>>().AsQueryable().OrderBy(_ => 0));

        var context = new CosmosDbContext(_client.Object, _settings.Object);

        _ = context.GetAsQueryable<TestEntity>(
            "items",
            "business-1",
            maxItemCount: 7,
            continuationToken: "next-page");

        Assert.Equal("next-page", capturedContinuationToken);
        Assert.NotNull(capturedOptions);
        Assert.Equal(7, capturedOptions.MaxItemCount);
    }

    public sealed class TestEntity : ICosmosDbEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public void AddCosmosDbContext_WithConnection_RegistersDatabaseSettings()
    {
        var services = new ServiceCollection();
        var key = Convert.ToBase64String("secret-key"u8.ToArray());
        var connection = new CosmosConnection("https://cosmos.test", key);

        services.AddCosmosDbContext(connection, "store-db");

        using var provider = services.BuildServiceProvider();
        var settings = provider.GetRequiredService<ICosmosDbSettings>();
        Assert.Equal("https://cosmos.test", settings.EndpointUri);
        Assert.Equal(key, settings.PrimaryKey);
        Assert.Equal("store-db", settings.DatabaseName);
        Assert.IsType<CosmosDbContext>(provider.GetRequiredService<ICosmosDbContext>());
    }
}
