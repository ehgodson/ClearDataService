using Clear.DataService.Abstractions;
using Clear.DataService.Contexts;
using Clear.DataService.Models;
using Clear.DataService.Utils;

namespace Clear.DataService.Examples;

/// <summary>
/// Example usage of the efficient pagination functionality in CosmosDbContext
/// Focus on continuation token-based pagination for optimal performance
/// </summary>
public class PaginationExamples
{
    private readonly ICosmosDbContext _cosmosDbContext;

    public PaginationExamples(ICosmosDbContext cosmosDbContext)
    {
        _cosmosDbContext = cosmosDbContext;
    }

    /// <summary>
    /// Example 1: Simple pagination without filtering
    /// </summary>
    public async Task<PagedResult<Product>> GetProductsPagedExample(
        string partitionKey,
        int pageSize = 50,
        string? continuationToken = null)
    {
        return await _cosmosDbContext.GetPagedList<Product>(
            containerName: "products",
            pageSize: pageSize,
            continuationToken: continuationToken,
            partitionKey: partitionKey
        );
    }

    /// <summary>
    /// Example 2: Pagination with filtering using Expression
    /// IMPORTANT: Always use the same predicate when using continuation tokens
    /// </summary>
    public async Task<PagedResult<Product>> GetFilteredProductsPagedExample(
        string partitionKey,
        decimal minPrice,
        int pageSize = 50,
        string? continuationToken = null)
    {
        return await _cosmosDbContext.GetPagedList<Product>(
            containerName: "products",
            filter: FilterBuilder<Product>.Create().And(p => p.Data.Price > minPrice), // MUST be identical across pagination calls
            pageSize: pageSize,
            continuationToken: continuationToken,
            partitionKey: partitionKey
        );
    }

    /// <summary>
    /// Example 3: Advanced pagination using SQL with parameters for better continuation token support
    /// </summary>
    public async Task<PagedResult<Product>> GetProductsWithSqlPaginationExample(
        string partitionKey,
        decimal minPrice,
        string category,
        int pageSize = 50,
        string? continuationToken = null)
    {
        var parameters = new Dictionary<string, object>
        {
            { "minPrice", minPrice },
            { "category", category }
        };

        return await _cosmosDbContext.GetPagedListWithSql<Product>(
            containerName: "products",
            whereClause: "c.data.price >= @minPrice AND c.data.category = @category",
            parameters: parameters,
            pageSize: pageSize,
            continuationToken: continuationToken,
            partitionKey: partitionKey
        );
    }

    /// <summary>
    /// Example 4: Working with full documents (includes metadata)
    /// </summary>
    public async Task<PagedCosmosResult<Product>> GetProductDocumentsPagedExample(
        string partitionKey,
        int pageSize = 50,
        string? continuationToken = null)
    {
        return await _cosmosDbContext.GetPagedDocuments<Product>(
            containerName: "products",
            pageSize: pageSize,
            continuationToken: continuationToken,
            partitionKey: partitionKey
        );
    }

    /// <summary>
    /// Example 5: Advanced scenario using the exposed GetAsQueryable for custom pagination
    /// </summary>
    public async Task<PagedCosmosResult<Product>> GetCustomQueryPagedExample(
        string partitionKey,
        int pageSize = 50,
        string? continuationToken = null)
    {
        // Build a complex query using LINQ
        var query = _cosmosDbContext
            .GetAsQueryable<Product>("products", partitionKey)
            .Where(doc => doc.Data.Price > 100)
            .OrderByDescending(doc => doc.Data.CreatedDate);

        // Convert to paged result
        return await query.ToPagedResult(pageSize, continuationToken);
    }

    /// <summary>
    /// Example 6: Iterating through all pages efficiently
    /// This is the recommended pattern for processing large datasets
    /// </summary>
    public async Task<List<Product>> GetAllProductsWithPaginationExample(string partitionKey, int pageSize = 100)
    {
        var allProducts = new List<Product>();
        string? continuationToken = null;

        do
        {
            var pageResult = await _cosmosDbContext.GetPagedList<Product>(
                containerName: "products",
                pageSize: pageSize,
                continuationToken: continuationToken,
                partitionKey: partitionKey
            );

            allProducts.AddRange(pageResult.Items);
            continuationToken = pageResult.ContinuationToken;

            // Optional: Log progress
            Console.WriteLine($"Loaded {pageResult.Count} products. Total so far: {allProducts.Count}. RU consumed: {pageResult.RequestCharge}");

        } while (!string.IsNullOrEmpty(continuationToken));

        return allProducts;
    }

    /// <summary>
    /// Example 7: Processing pages without loading all into memory (streaming pattern)
    /// Best for very large datasets where you want to process items without storing them all
    /// </summary>
    public async Task ProcessAllProductsStreamingExample(
        string partitionKey,
        Func<Product, Task> processItem,
        int pageSize = 100)
    {
        string? continuationToken = null;
        var totalProcessed = 0;

        do
        {
            var pageResult = await _cosmosDbContext.GetPagedList<Product>(
                containerName: "products",
                pageSize: pageSize,
                continuationToken: continuationToken,
                partitionKey: partitionKey
            );

            // Process each item without storing in memory
            foreach (var product in pageResult.Items)
            {
                await processItem(product);
                totalProcessed++;
            }

            continuationToken = pageResult.ContinuationToken;

            Console.WriteLine($"Processed {pageResult.Count} products. Total processed: {totalProcessed}. RU consumed: {pageResult.RequestCharge}");

        } while (!string.IsNullOrEmpty(continuationToken));
    }

    /// <summary>
    /// Example 8: Simple next/previous navigation pattern
    /// Perfect for UI with "Load More" or "Next/Previous" buttons
    /// </summary>
    public async Task<NavigationResult> GetNextPageExample(
        string partitionKey,
        string? continuationToken = null,
        string? category = null,
        int pageSize = 20)
    {
        PagedResult<Product> result;

        if (string.IsNullOrEmpty(category))
        {
            result = await _cosmosDbContext.GetPagedList<Product>(
                containerName: "products",
                pageSize: pageSize,
                continuationToken: continuationToken,
                partitionKey: partitionKey
            );
        }
        else
        {
            // IMPORTANT: Always use the same filter with continuation tokens
            result = await _cosmosDbContext.GetPagedList<Product>(
                containerName: "products",
                filter: FilterBuilder<Product>.Create().And(p => p.Data.Category == category),
                pageSize: pageSize,
                continuationToken: continuationToken,
                partitionKey: partitionKey
            );
        }

        return new NavigationResult
        {
            Items = result.Items,
            NextPageToken = result.ContinuationToken,
            HasMoreResults = result.HasMoreResults,
            ItemCount = result.Count,
            RequestCharge = result.RequestCharge
        };
    }
}

/// <summary>
/// Simple navigation result for next/previous pagination
/// </summary>
public class NavigationResult
{
    public List<Product> Items { get; set; } = [];
    public string? NextPageToken { get; set; }
    public bool HasMoreResults { get; set; }
    public int ItemCount { get; set; }
    public double RequestCharge { get; set; }
}