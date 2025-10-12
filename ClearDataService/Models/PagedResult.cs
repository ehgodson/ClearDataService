using Clear.DataService.Abstractions;
using Clear.DataService.Entities.Cosmos;

namespace Clear.DataService.Models;

/// <summary>
/// Represents a page of results with continuation token support for pagination
/// </summary>
/// <typeparam name="T">The type of items in the page</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// The items in the current page
    /// </summary>
    public List<T> Items { get; init; } = [];

    /// <summary>
    /// Token to retrieve the next page of results, null if no more pages
    /// </summary>
    public string? ContinuationToken { get; init; }

    /// <summary>
    /// Indicates if there are more results available
    /// </summary>
    public bool HasMoreResults { get; init; }

    /// <summary>
    /// Number of items in the current page
    /// </summary>
    public int Count { get; init; }

    /// <summary>
    /// Request charge (RU) consumed for this page
    /// </summary>
    public double RequestCharge { get; init; }

    /// <summary>
    /// Creates an empty paged result
    /// </summary>
    public static PagedResult<T> Empty() => new()
    {
        Items = [],
        ContinuationToken = null,
        HasMoreResults = false,
        Count = 0,
        RequestCharge = 0
    };
}

/// <summary>
/// Specialized paged result for Cosmos DB documents
/// </summary>
/// <typeparam name="T">The entity type that implements ICosmosDbEntity</typeparam>
public class PagedCosmosResult<T> : PagedResult<CosmosDbDocument<T>> where T : ICosmosDbEntity
{
    /// <summary>
    /// Creates a paged Cosmos DB result from response data
    /// </summary>
    public static PagedCosmosResult<T> Create(
        IEnumerable<CosmosDbDocument<T>> items,
        string? continuationToken,
        double requestCharge)
    {
        var itemsList = items.ToList();
        return new PagedCosmosResult<T>
        {
            Items = itemsList,
            ContinuationToken = continuationToken,
            HasMoreResults = !string.IsNullOrEmpty(continuationToken),
            Count = itemsList.Count,
            RequestCharge = requestCharge
        };
    }

    /// <summary>
    /// Creates an empty paged Cosmos DB result
    /// </summary>
    public new static PagedCosmosResult<T> Empty() => new()
    {
        Items = [],
        ContinuationToken = null,
        HasMoreResults = false,
        Count = 0,
        RequestCharge = 0
    };
}