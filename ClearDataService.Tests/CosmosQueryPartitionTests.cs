using Clear.DataService.Abstractions;
using Clear.DataService.Contexts;
using Clear.DataService.Entities.Cosmos;
using Clear.DataService.Models;
using Clear.DataService.Utils;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Moq;

namespace ClearDataService.Tests;

public sealed class CosmosQueryPartitionTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void QueryOptionsPreserveOmittedSingleAndHierarchicalKeys(int levels)
    {
        var key = Key(levels);
        QueryRequestOptions? captured = null;
        var container = new Mock<Container>();
        container.Setup(c => c.GetItemLinqQueryable<CosmosDbDocument<Account>>(
                false, It.IsAny<string>(), It.IsAny<QueryRequestOptions>(), It.IsAny<CosmosLinqSerializerOptions>()))
            .Callback((bool _, string token, QueryRequestOptions options, CosmosLinqSerializerOptions serializer) => captured = options)
            .Returns(Array.Empty<CosmosDbDocument<Account>>().AsQueryable().OrderBy(d => d.Id));

        _ = Context(container).GetAsQueryable<Account>("accounts", key, maxItemCount: 7, continuationToken: "next");

        Assert.NotNull(captured);
        Assert.Equal(key?.ToCosmosPartitionKey(), captured.PartitionKey);
        Assert.Equal(7, captured.MaxItemCount);
    }

    [Fact]
    public void UnpartitionedLookupIncludesTenantEmailAndCanonicalUserPartitions()
    {
        var documents = new[] { "tenant", "account@example.com", "canonical-user-id" }
            .Select(key => CosmosDbDocument<Account>.Create(new Account { Id = "user", Email = "account@example.com" }, key)).ToArray();
        var container = new Mock<Container>();
        container.Setup(c => c.GetItemLinqQueryable<CosmosDbDocument<Account>>(
                false, It.IsAny<string>(), It.IsAny<QueryRequestOptions>(), It.IsAny<CosmosLinqSerializerOptions>()))
            .Returns((bool _, string token, QueryRequestOptions options, CosmosLinqSerializerOptions serializer) =>
            {
                // Simulate SDK partition routing before LINQ evaluation. None must not
                // be treated as a wildcard over the three named partitions.
                var routed = documents.Where(d => options.PartitionKey == null ||
                    options.PartitionKey == new PartitionKey(d.PartitionKey));
                return routed.AsQueryable().OrderBy(d => d.Id);
            });
        var context = Context(container);
        var filter = FilterBuilder<Account>.New(d => d.Data.Email == "account@example.com");

        Assert.Equal(3, context.GetAsQueryable("accounts", filter: filter).Count());
        Assert.Equal("tenant", Assert.Single(context.GetAsQueryable("accounts", "tenant", filter)).PartitionKey);
    }

    [Theory]
    [InlineData("entity")]
    [InlineData("document")]
    [InlineData("id-discovery")]
    [InlineData("entities")]
    [InlineData("documents")]
    [InlineData("entity-page")]
    [InlineData("document-page")]
    public async Task LinqReadEntryPointsLeaveUnspecifiedPartitionUnset(string operation)
    {
        QueryRequestOptions? captured = null;
        var container = new Mock<Container>();
        container.Setup(c => c.GetItemLinqQueryable<CosmosDbDocument<Account>>(
                false, It.IsAny<string>(), It.IsAny<QueryRequestOptions>(), It.IsAny<CosmosLinqSerializerOptions>()))
            .Callback((bool _, string token, QueryRequestOptions options, CosmosLinqSerializerOptions serializer) => captured = options)
            .Throws(new QueryCapturedException()); // Stop before network-dependent iterator execution.
        var context = Context(container);
        var filter = FilterBuilder<Account>.New(d => d.Data.Email == "account@example.com");

        await Assert.ThrowsAsync<QueryCapturedException>(async () =>
        {
            switch (operation)
            {
                case "entity": await context.Get("accounts", filter); break;
                case "document": await context.GetDocument("accounts", filter); break;
                case "id-discovery": await context.GetDocument<Account>("accounts", "user"); break;
                case "entities": await context.GetList<Account>("accounts", filter: filter); break;
                case "documents": await context.GetDocuments<Account>("accounts", filter: filter); break;
                case "entity-page": await context.GetPagedList<Account>("accounts", filter: filter); break;
                case "document-page": await context.GetPagedDocuments<Account>("accounts", filter: filter); break;
                default: throw new ArgumentOutOfRangeException(nameof(operation));
            }
        });
        Assert.NotNull(captured);
        Assert.Null(captured.PartitionKey);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    public async Task SqlPagesPreserveCrossPartitionAndExplicitKeyBehavior(bool documents, int levels)
    {
        var key = Key(levels);
        QueryRequestOptions? captured = null;
        var iterator = new Mock<FeedIterator<CosmosDbDocument<Account>>>();
        iterator.SetupGet(i => i.HasMoreResults).Returns(false);
        var container = new Mock<Container>();
        container.Setup(c => c.GetItemQueryIterator<CosmosDbDocument<Account>>(
                It.IsAny<QueryDefinition>(), "next", It.IsAny<QueryRequestOptions>()))
            .Callback((QueryDefinition query, string token, QueryRequestOptions options) => captured = options)
            .Returns(iterator.Object);
        var context = Context(container);
        if (documents)
            await context.GetPagedDocumentsWithSql<Account>("accounts", pageSize: 7, continuationToken: "next", partitionKey: key);
        else
            await context.GetPagedListWithSql<Account>("accounts", pageSize: 7, continuationToken: "next", partitionKey: key);
        Assert.NotNull(captured);
        Assert.Equal(key?.ToCosmosPartitionKey(), captured.PartitionKey);
        Assert.Equal(7, captured.MaxItemCount);
    }

    [Theory]
    [InlineData(1, false, false)]
    [InlineData(2, false, true)]
    [InlineData(2, true, true)]
    public async Task ProjectedPagePreservesPartitionAndContinuation(int levels, bool empty, bool hasContinuation)
    {
        var partition = Key(levels)!.ToCosmosPartitionKey();
        var query = new QueryDefinition("SELECT c.data.email AS email FROM c WHERE c.id = @id")
            .WithParameter("@id", "user");
        using var cancellation = new CancellationTokenSource();
        string? inputToken = levels == 1 ? null : "resume";
        string? outputToken = hasContinuation ? "next" : null;
        EmailProjection[] rows = empty ? [] : [new("account@example.com")];
        var response = new Mock<FeedResponse<EmailProjection>>();
        response.Setup(r => r.GetEnumerator()).Returns(() => rows.AsEnumerable().GetEnumerator());
        response.SetupGet(r => r.Count).Returns(rows.Length);
        response.SetupGet(r => r.ContinuationToken).Returns(outputToken!);
        response.SetupGet(r => r.RequestCharge).Returns(2.75);
        var iterator = new Mock<FeedIterator<EmailProjection>>();
        iterator.Setup(i => i.ReadNextAsync(cancellation.Token)).ReturnsAsync(response.Object);
        var container = new Mock<Container>(MockBehavior.Strict);
        container.Setup(c => c.GetItemQueryIterator<EmailProjection>(query, inputToken,
                It.Is<QueryRequestOptions>(options => options.PartitionKey == partition && options.MaxItemCount == 7)))
            .Returns(iterator.Object);

        ICosmosDbContext context = Context(container);
        var page = await context.ReadProjectedPageAsync<EmailProjection>("accounts", query, partition,
            pageSize: 7, continuationToken: inputToken, cancellationToken: cancellation.Token);

        Assert.Equal(rows, page.Items.ToArray());
        Assert.Equal(rows.Length, page.Count);
        Assert.Equal(outputToken, page.ContinuationToken);
        Assert.Equal(hasContinuation, page.HasMoreResults);
        Assert.Equal(2.75, page.RequestCharge);
        iterator.Verify(i => i.ReadNextAsync(cancellation.Token), Times.Once);
    }

    public sealed record EmailProjection(string Email);

    private static CosmosDbContext Context(Mock<Container> container)
    {
        var client = new Mock<CosmosClient>();
        client.Setup(c => c.GetContainer("database", "accounts")).Returns(container.Object);
        var settings = new Mock<ICosmosDbSettings>();
        settings.SetupGet(s => s.DatabaseName).Returns("database");
        return new CosmosDbContext(client.Object, settings.Object);
    }

    private static CosmosDbPartitionKey? Key(int levels) => levels switch
    {
        0 => null,
        1 => CosmosDbPartitionKey.Create("tenant"),
        2 => CosmosDbPartitionKey.Create("tenant", "user"),
        3 => CosmosDbPartitionKey.Create("tenant", "user", "document"),
        _ => throw new ArgumentOutOfRangeException(nameof(levels))
    };

    private sealed class QueryCapturedException : Exception;

    public sealed class Account : ICosmosDbEntity
    {
        public string Id { get; set; } = "user";
        public string Email { get; set; } = "account@example.com";
    }
}
