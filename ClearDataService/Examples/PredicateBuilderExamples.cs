using Clear.DataService.Abstractions;
using Clear.DataService.Models;
using Clear.DataService.Utils;

namespace Clear.DataService.Examples;

/// <summary>
/// Examples showing how to use FilterBuilder for dynamic filtering
/// </summary>
public class FilterBuilderExamples
{
    private readonly ICosmosDbContext _cosmosDbContext;

    public FilterBuilderExamples(ICosmosDbContext cosmosDbContext)
    {
        _cosmosDbContext = cosmosDbContext;
    }

    /// <summary>
    /// Example 1: Building filters based on optional parameters
    /// </summary>
    public async Task<List<Product>> GetProductsWithDynamicFilters(
        string partitionKey,
        string? category = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        bool? isActive = null,
        DateTime? createdAfter = null)
    {
        // Build filter dynamically based on provided parameters
        var filter = FilterBuilder<Product>.Create()
            .And(p => p.Data.Category == category!, !string.IsNullOrEmpty(category))
            .And(p => p.Data.Price >= minPrice.Value, minPrice.HasValue)
            .And(p => p.Data.Price <= maxPrice.Value, maxPrice.HasValue)
            .And(p => p.Data.IsActive == isActive.Value, isActive.HasValue)
            .And(p => p.Data.CreatedDate >= createdAfter.Value, createdAfter.HasValue);

        // Use GetDocuments with FilterBuilder for efficient database-level filtering
        var documents = await _cosmosDbContext.GetDocuments<Product>(
            containerName: "products",
            filter: filter, // FilterBuilder converts implicitly to Expression
            partitionKey: partitionKey
        );
        
        // Extract the data from the documents
        return documents.Select(doc => doc.Data).ToList();
    }

    /// <summary>
    /// Example 2: Using FilterBuilder with pagination
    /// </summary>
    public async Task<PagedResult<Product>> GetProductsPagedWithFilters(
        string partitionKey,
        ProductFilterRequest filterRequest,
        int pageSize = 50,
        string? continuationToken = null)
    {
        var filter = FilterBuilder<Product>.Create()
            .And(p => p.Data.Category == filterRequest.Category!, !string.IsNullOrEmpty(filterRequest.Category))
            .And(p => p.Data.Name.Contains(filterRequest.Name!), !string.IsNullOrEmpty(filterRequest.Name))
            .And(p => p.Data.Price >= filterRequest.MinPrice.Value, filterRequest.MinPrice.HasValue)
            .And(p => p.Data.Price <= filterRequest.MaxPrice.Value, filterRequest.MaxPrice.HasValue)
            .And(p => p.Data.IsActive == filterRequest.IsActive.Value, filterRequest.IsActive.HasValue);

        return await _cosmosDbContext.GetPagedList<Product>(
            containerName: "products",
            filter: filter,
            pageSize: pageSize,
            continuationToken: continuationToken,
            partitionKey: partitionKey
        );
    }

    /// <summary>
    /// Example 3: Complex filtering with OR conditions
    /// </summary>
    public async Task<List<Product>> GetProductsWithComplexFilters(
        string partitionKey,
        string? searchTerm = null,
        List<string>? categories = null,
        List<string>? brands = null)
    {
        var filter = FilterBuilder<Product>.Create();

        // Search term can match either name or category
        if (!string.IsNullOrEmpty(searchTerm))
        {
            var searchFilter = FilterBuilder<Product>.Create()
                .Or(p => p.Data.Name.Contains(searchTerm))
                .Or(p => p.Data.Category.Contains(searchTerm));

            filter.And(searchFilter);
        }

        // Must match at least one of the specified categories
        if (categories?.Any() == true)
        {
            var categoryFilter = FilterBuilder<Product>.Create();
            foreach (var category in categories)
            {
                categoryFilter.Or(p => p.Data.Category == category);
            }
            filter.And(categoryFilter);
        }

        // Use GetDocuments for FilterBuilder-based filtering at database level
        var documents = await _cosmosDbContext.GetDocuments<Product>(
            containerName: "products",
            filter: filter, // FilterBuilder converts implicitly to Expression
            partitionKey: partitionKey
        );
        
        // Extract the data from the documents
        return documents.Select(doc => doc.Data).ToList();
    }

    /// <summary>
    /// Example 4: Building SQL WHERE clauses dynamically for SQL-based pagination
    /// </summary>
    public async Task<PagedResult<Product>> GetProductsWithSqlFilters(
        string partitionKey,
        ProductFilterRequest filterRequest,
        int pageSize = 50,
        string? continuationToken = null)
    {
        var sqlBuilder = new SqlPredicateBuilder();

        sqlBuilder
            .And(!string.IsNullOrEmpty(filterRequest.Category), "c.data.category = @category", "category", filterRequest.Category!)
            .And(!string.IsNullOrEmpty(filterRequest.Name), "CONTAINS(c.data.name, @name)", "name", filterRequest.Name!)
            .And(filterRequest.MinPrice.HasValue, "c.data.price >= @minPrice", "minPrice", filterRequest.MinPrice.Value)
            .And(filterRequest.MaxPrice.HasValue, "c.data.price <= @maxPrice", "maxPrice", filterRequest.MaxPrice.Value)
            .And(filterRequest.IsActive.HasValue, "c.data.isActive = @isActive", "isActive", filterRequest.IsActive.Value);

        return await _cosmosDbContext.GetPagedListWithSql<Product>(
            containerName: "products",
            whereClause: sqlBuilder.GetWhereClause(),
            parameters: sqlBuilder.GetParameters(),
            pageSize: pageSize,
            continuationToken: continuationToken,
            partitionKey: partitionKey
        );
    }

    /// <summary>
    /// Example 5: Using extension methods for cleaner syntax
    /// </summary>
    public async Task<List<Product>> GetProductsWithExtensionFilters(
        string partitionKey,
        string? category = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        bool? isActive = null)
    {
        var filter = FilterBuilder<Product>.Create()
            .ByCategory(category)
            .ByPriceRange(minPrice, maxPrice)
            .ByActiveStatus(isActive);

        // Use GetDocuments with FilterBuilder for efficient database-level filtering
        var documents = await _cosmosDbContext.GetDocuments<Product>(
            containerName: "products",
            filter: filter, // FilterBuilder converts implicitly to Expression
            partitionKey: partitionKey
        );
        
        // Extract the data from the documents
        return documents.Select(doc => doc.Data).ToList();
    }
}

/// <summary>
/// Helper class for building SQL WHERE clauses dynamically
/// </summary>
public class SqlPredicateBuilder
{
    private readonly List<string> _conditions = [];
    private readonly Dictionary<string, object> _parameters = [];

    public SqlPredicateBuilder And(bool condition, string sqlCondition, string parameterName, object parameterValue)
    {
        if (condition)
        {
            _conditions.Add(sqlCondition);
            _parameters[parameterName] = parameterValue;
        }
        return this;
    }

    public SqlPredicateBuilder Or(bool condition, string sqlCondition, string parameterName, object parameterValue)
    {
        if (condition)
        {
            // For OR conditions, you might need more complex logic depending on requirements
            _conditions.Add($"OR ({sqlCondition})");
            _parameters[parameterName] = parameterValue;
        }
        return this;
    }

    public string GetWhereClause()
    {
        return _conditions.Count > 0 ? string.Join(" AND ", _conditions) : "";
    }

    public Dictionary<string, object> GetParameters()
    {
        return _parameters;
    }

    public bool HasConditions => _conditions.Count > 0;
}

/// <summary>
/// Example filter request model
/// </summary>
public class ProductFilterRequest
{
    public string? Category { get; set; }
    public string? Name { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }
}

/// <summary>
/// Static class containing reusable filter methods for Product
/// </summary>
public static class ProductFilters
{
    public static FilterBuilder<Product> ByCategory(this FilterBuilder<Product> builder, string? category)
    {
        return builder.And(p => p.Data.Category == category, !string.IsNullOrEmpty(category));
    }

    public static FilterBuilder<Product> ByPriceRange(this FilterBuilder<Product> builder, decimal? minPrice, decimal? maxPrice)
    {
        return builder
            .And(p => p.Data.Price >= minPrice.Value, minPrice.HasValue)
            .And(p => p.Data.Price <= maxPrice.Value, maxPrice.HasValue);
    }

    public static FilterBuilder<Product> ByActiveStatus(this FilterBuilder<Product> builder, bool? isActive)
    {
        return builder.And(p => p.Data.IsActive == isActive.Value, isActive.HasValue);
    }

    public static FilterBuilder<Product> ByDateRange(this FilterBuilder<Product> builder, DateTime? from, DateTime? to)
    {
        return builder
            .And(p => p.Data.CreatedDate >= from.Value, from.HasValue)
            .And(p => p.Data.CreatedDate <= to.Value, to.HasValue);
    }
}