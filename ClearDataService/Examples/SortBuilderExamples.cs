using Clear.DataService.Abstractions;
using Clear.DataService.Entities.Cosmos;
using Clear.DataService.Models;
using Clear.DataService.Utils;

namespace Clear.DataService.Examples;

/// <summary>
/// Examples demonstrating how to use SortBuilder with pagination methods
/// </summary>
public class SortBuilderExamples
{
    private readonly ICosmosDbContext _cosmosDbContext;

    public SortBuilderExamples(ICosmosDbContext cosmosDbContext)
    {
        _cosmosDbContext = cosmosDbContext;
    }

    /// <summary>
    /// Example entity for demonstration
    /// </summary>
    public class Product : ICosmosDbEntity
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Category { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int Stock { get; set; }
    }

    /// <summary>
    /// Example 1: Simple ascending sort by name
    /// </summary>
    public async Task<PagedResult<Product>> GetProductsSortedByNameAsync(
        string containerName, 
        int pageSize = 20, 
        string? continuationToken = null)
    {
        // Example 1: Simple ascending sort
        var sortByName = SortBuilder<Product>
            .New(doc => doc.Data.Name); // Default is ascending

        return await _cosmosDbContext.GetPagedList<Product>(
            containerName: containerName,
            pageSize: pageSize,
            continuationToken: continuationToken,
            partitionKey: null,
            sortBuilder: sortByName
        );
    }

    /// <summary>
    /// Example 2: Descending sort by price
    /// </summary>
    public async Task<PagedResult<Product>> GetProductsSortedByPriceDescAsync(
        string containerName, 
        int pageSize = 20, 
        string? continuationToken = null)
    {
        var sortBuilder = SortBuilder<Product>
            .New(doc => doc.Data.Price, SortDirection.Descending);

        return await _cosmosDbContext.GetPagedList<Product>(
            containerName: containerName,
            pageSize: pageSize,
            continuationToken: continuationToken,
            partitionKey: null,
            sortBuilder: sortBuilder
        );
    }

    /// <summary>
    /// Example 3: Multiple sort criteria - by category ascending, then by price descending
    /// </summary>
    public async Task<PagedResult<Product>> GetProductsWithMultipleSortAsync(
        string containerName, 
        int pageSize = 20, 
        string? continuationToken = null)
    {
        var sortBuilder = SortBuilder<Product>
            .Create()
            .ThenBy(doc => doc.Data.Category)                    // First by category (ascending)
            .ThenByDescending(doc => doc.Data.Price)             // Then by price (descending)
            .ThenBy(doc => doc.Data.Name);                       // Finally by name (ascending)

        return await _cosmosDbContext.GetPagedList<Product>(
            containerName: containerName,
            pageSize: pageSize,
            continuationToken: continuationToken,
            partitionKey: null,
            sortBuilder: sortBuilder
        );
    }

    /// <summary>
    /// Example 4: Conditional sorting based on user preferences
    /// </summary>
    public async Task<PagedResult<Product>> GetProductsWithConditionalSortAsync(
        string containerName,
        bool sortByPrice = false,
        bool sortByDate = false,
        bool descending = false,
        int pageSize = 20,
        string? continuationToken = null)
    {
        var direction = descending ? SortDirection.Descending : SortDirection.Ascending;

        var sortBuilder = SortBuilder<Product>
            .Create()
            .ThenBy(sortByPrice, doc => doc.Data.Price, direction)
            .ThenBy(sortByDate, doc => doc.Data.CreatedDate, direction)
            .ThenBy(doc => doc.Data.Name); // Always sort by name as fallback

        return await _cosmosDbContext.GetPagedList<Product>(
            containerName: containerName,
            pageSize: pageSize,
            continuationToken: continuationToken,
            partitionKey: null,
            sortBuilder: sortBuilder
        );
    }

    /// <summary>
    /// Example 5: Combining filtering with sorting
    /// </summary>
    public async Task<PagedResult<Product>> GetActiveProductsSortedAsync(
        string containerName,
        decimal? minPrice = null,
        string? category = null,
        int pageSize = 20,
        string? continuationToken = null)
    {
        // Build filter predicate
        var filterBuilder = FilterBuilder<Product>
            .Create()
            .And(doc => doc.Data.IsActive)                                    // Always filter active products
            .And(doc => doc.Data.Price >= minPrice!.Value, minPrice.HasValue) // Conditionally filter by price
            .And(doc => doc.Data.Category == category!, !string.IsNullOrEmpty(category)); // Conditionally filter by category

        // Build sort criteria
        var sortBuilder = SortBuilder<Product>
            .Create()
            .ThenBy(doc => doc.Data.Category)
            .ThenByDescending(doc => doc.Data.Price)
            .ThenBy(doc => doc.Data.Name);

        return await _cosmosDbContext.GetPagedList<Product>(
            containerName: containerName,
            filter: filterBuilder,
            pageSize: pageSize,
            continuationToken: continuationToken,
            partitionKey: null,
            sortBuilder: sortBuilder
        );
    }

    /// <summary>
    /// Example 6: Using SQL-based pagination with sorting
    /// </summary>
    public async Task<PagedResult<Product>> GetProductsWithSqlSortingAsync(
        string containerName,
        string? searchTerm = null,
        decimal? minPrice = null,
        int pageSize = 20,
        string? continuationToken = null)
    {
        var whereClause = "c.data.isActive = true";
        var parameters = new Dictionary<string, object>();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            whereClause += " AND CONTAINS(LOWER(c.data.name), @searchTerm)";
            parameters["searchTerm"] = searchTerm.ToLower();
        }

        if (minPrice.HasValue)
        {
            whereClause += " AND c.data.price >= @minPrice";
            parameters["minPrice"] = minPrice.Value;
        }

        // Build sort criteria for SQL query
        var sortBuilder = SortBuilder<Product>
            .Create()
            .ThenBy(doc => doc.Data.Category)
            .ThenByDescending(doc => doc.Data.Price)
            .ThenBy(doc => doc.Data.CreatedDate);

        return await _cosmosDbContext.GetPagedListWithSql<Product>(
            containerName: containerName,
            whereClause: whereClause,
            parameters: parameters,
            pageSize: pageSize,
            continuationToken: continuationToken,
            partitionKey: null,
            sortBuilder: sortBuilder
        );
    }

    /// <summary>
    /// Example 7: Working with Cosmos documents directly with sorting
    /// </summary>
    public async Task<PagedCosmosResult<Product>> GetProductDocumentsWithSortingAsync(
        string containerName,
        int pageSize = 20,
        string? continuationToken = null)
    {
        // Sort directly on document properties
        var sortBuilder = SortBuilder<Product>
            .Create()
            .ThenBy(doc => doc.Data.Category)
            .ThenByDescending(doc => doc.Data.Price)
            .ThenBy(doc => doc.Data.CreatedDate);

        return await _cosmosDbContext.GetPagedDocuments<Product>(
            containerName: containerName,
            pageSize: pageSize,
            continuationToken: continuationToken,
            partitionKey: null,
            sortBuilder: sortBuilder
        );
    }

    /// <summary>
    /// Example 8: No sorting (uses default/natural ordering)
    /// </summary>
    public async Task<PagedResult<Product>> GetProductsWithoutSortingAsync(
        string containerName,
        int pageSize = 20,
        string? continuationToken = null)
    {
        // No sort builder passed - uses natural/default ordering
        return await _cosmosDbContext.GetPagedList<Product>(
            containerName: containerName,
            pageSize: pageSize,
            continuationToken: continuationToken,
            partitionKey: null
            // sortBuilder: null (default)
        );
    }

    /// <summary>
    /// Example 9: Building sort criteria dynamically
    /// </summary>
    public async Task<PagedResult<Product>> GetProductsWithDynamicSortingAsync(
        string containerName,
        Dictionary<string, SortDirection> sortCriteria,
        int pageSize = 20,
        string? continuationToken = null)
    {
        var sortBuilder = SortBuilder<Product>.Create();

        foreach (var criteria in sortCriteria)
        {
            switch (criteria.Key.ToLower())
            {
                case "name":
                    sortBuilder.ThenBy(doc => doc.Data.Name, criteria.Value);
                    break;
                case "price":
                    sortBuilder.ThenBy(doc => doc.Data.Price, criteria.Value);
                    break;
                case "category":
                    sortBuilder.ThenBy(doc => doc.Data.Category, criteria.Value);
                    break;
                case "created":
                case "createddate":
                    sortBuilder.ThenBy(doc => doc.Data.CreatedDate, criteria.Value);
                    break;
                case "stock":
                    sortBuilder.ThenBy(doc => doc.Data.Stock, criteria.Value);
                    break;
            }
        }

        // If no valid criteria were added, add a default sort
        if (!sortBuilder.HasSortCriteria)
        {
            sortBuilder.ThenBy(doc => doc.Data.Name);
        }

        return await _cosmosDbContext.GetPagedList<Product>(
            containerName: containerName,
            pageSize: pageSize,
            continuationToken: continuationToken,
            partitionKey: null,
            sortBuilder: sortBuilder
        );
    }
}