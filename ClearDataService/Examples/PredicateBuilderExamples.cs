using Clear.DataService.Abstractions;
using Clear.DataService.Models;
using Clear.DataService.Utils;

namespace Clear.DataService.Examples;

/// <summary>
/// Examples showing how to use PredicateBuilder for dynamic filtering
/// </summary>
public class PredicateBuilderExamples
{
    private readonly ICosmosDbContext _cosmosDbContext;

    public PredicateBuilderExamples(ICosmosDbContext cosmosDbContext)
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
        // Build predicate dynamically based on provided parameters
        var predicate = PredicateBuilder<Product>.Create()
            .And(!string.IsNullOrEmpty(category), p => p.Category == category!)
            .And(minPrice.HasValue, p => p.Price >= minPrice.Value)
            .And(maxPrice.HasValue, p => p.Price <= maxPrice.Value)
            .And(isActive.HasValue, p => p.IsActive == isActive.Value)
            .And(createdAfter.HasValue, p => p.CreatedDate >= createdAfter.Value)
            .Build();

        return await _cosmosDbContext.GetList<Product>(
            containerName: "products",
            predicate: predicate.Compile(), // Convert Expression to Func for in-memory filtering
            partitionKey: partitionKey
        );
    }

    /// <summary>
    /// Example 2: Using PredicateBuilder with pagination
    /// </summary>
    public async Task<PagedResult<Product>> GetProductsPagedWithFilters(
        string partitionKey,
        ProductFilterRequest filterRequest,
        int pageSize = 50,
        string? continuationToken = null)
    {
        var predicate = PredicateBuilder<Product>.Create()
            .And(!string.IsNullOrEmpty(filterRequest.Category), p => p.Category == filterRequest.Category!)
            .And(!string.IsNullOrEmpty(filterRequest.Name), p => p.Name.Contains(filterRequest.Name!))
            .And(filterRequest.MinPrice.HasValue, p => p.Price >= filterRequest.MinPrice.Value)
            .And(filterRequest.MaxPrice.HasValue, p => p.Price <= filterRequest.MaxPrice.Value)
            .And(filterRequest.IsActive.HasValue, p => p.IsActive == filterRequest.IsActive.Value)
            .Build();

        return await _cosmosDbContext.GetPagedList<Product>(
            containerName: "products",
            predicate: predicate,
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
        var predicate = PredicateBuilder<Product>.Create();

        // Search term can match either name or category
        if (!string.IsNullOrEmpty(searchTerm))
        {
            var searchPredicate = PredicateBuilder<Product>.Create()
                .Or(p => p.Name.Contains(searchTerm))
                .Or(p => p.Category.Contains(searchTerm))
                .Build();

            predicate.And(searchPredicate);
        }

        // Must match at least one of the specified categories
        if (categories?.Any() == true)
        {
            var categoryPredicate = PredicateBuilder<Product>.Create();
            foreach (var category in categories)
            {
                categoryPredicate.Or(p => p.Category == category);
            }
            predicate.And(categoryPredicate.Build());
        }

        return await _cosmosDbContext.GetList<Product>(
            containerName: "products",
            predicate: predicate.Build().Compile(),
            partitionKey: partitionKey
        );
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
        var predicate = PredicateBuilder<Product>.Create()
            .ByCategory(category)
            .ByPriceRange(minPrice, maxPrice)
            .ByActiveStatus(isActive)
            .Build();

        return await _cosmosDbContext.GetList<Product>(
            containerName: "products",
            predicate: predicate.Compile(),
            partitionKey: partitionKey
        );
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
    public static PredicateBuilder<Product> ByCategory(this PredicateBuilder<Product> builder, string? category)
    {
        return builder.And(!string.IsNullOrEmpty(category), p => p.Category == category);
    }

    public static PredicateBuilder<Product> ByPriceRange(this PredicateBuilder<Product> builder, decimal? minPrice, decimal? maxPrice)
    {
        return builder
            .And(minPrice.HasValue, p => p.Price >= minPrice.Value)
            .And(maxPrice.HasValue, p => p.Price <= maxPrice.Value);
    }

    public static PredicateBuilder<Product> ByActiveStatus(this PredicateBuilder<Product> builder, bool? isActive)
    {
        return builder.And(isActive.HasValue, p => p.IsActive == isActive.Value);
    }

    public static PredicateBuilder<Product> ByDateRange(this PredicateBuilder<Product> builder, DateTime? from, DateTime? to)
    {
        return builder
            .And(from.HasValue, p => p.CreatedDate >= from.Value)
            .And(to.HasValue, p => p.CreatedDate <= to.Value);
    }
}