# Efficient Pagination Support in ClearDataService

This document describes the efficient pagination functionality in CosmosDbContext, focusing on continuation token-based pagination for optimal performance and cost.

## Overview

The ClearDataService supports efficient pagination for large datasets using Cosmos DB's native continuation tokens. This approach:
- **Minimizes Request Unit (RU) consumption**
- **Reduces memory usage** by loading one page at a time
- **Provides optimal performance** without expensive count operations
- **Maintains backward compatibility** with existing code

## Core Principle

**Use continuation tokens for sequential navigation** - this is the most efficient approach for Cosmos DB pagination. Avoid expensive operations like counting total records or jumping to arbitrary page numbers.

## Available Methods

### Basic Pagination Methods

#### GetPagedList<T>
Returns a page of entities with continuation token support:

```csharp
// Simple pagination
var result = await cosmosContext.GetPagedList<Product>(
    containerName: "products",
    pageSize: 50,
    continuationToken: null, // First page
    partitionKey: "electronics"
);

// With filtering (IMPORTANT: Keep same predicate with continuation tokens)
var result = await cosmosContext.GetPagedList<Product>(
    containerName: "products",
    predicate: p => p.Price > 100, // MUST be identical across pages
    pageSize: 50,
    continuationToken: tokenFromPreviousPage,
    partitionKey: "electronics"
);
```

#### GetPagedDocuments<T>
Returns full CosmosDbDocument<T> objects with metadata:

```csharp
var result = await cosmosContext.GetPagedDocuments<Product>(
    containerName: "products", 
    pageSize: 50,
    continuationToken: tokenFromPreviousPage,
    partitionKey: "electronics"
);
```

### Advanced SQL-Based Pagination

For complex queries with reliable continuation token support:

```csharp
var result = await cosmosContext.GetPagedListWithSql<Product>(
    containerName: "products",
    whereClause: "c.data.price >= @minPrice AND c.data.category = @category",
    parameters: new Dictionary<string, object> 
    {
        { "minPrice", 100m },
        { "category", "Electronics" }
    },
    pageSize: 50,
    continuationToken: tokenFromPreviousPage,
    partitionKey: "electronics"
);
```

### Custom Queries with GetAsQueryable

For advanced LINQ scenarios:

```csharp
var query = cosmosContext.GetAsQueryable<Product>("products", "electronics")
    .Where(doc => doc.Data.Price > 100)
    .OrderBy(doc => doc.Data.Name);

var result = await query.ToPagedResult(pageSize: 25, continuationToken: token);
```

## Return Types

### PagedResult<T>
Contains entities and pagination metadata:

```csharp
public class PagedResult<T>
{
    public List<T> Items { get; init; }              // Current page items
    public string? ContinuationToken { get; init; }   // Token for next page
    public bool HasMoreResults { get; init; }         // True if more pages exist
    public int Count { get; init; }                   // Items in current page
    public double RequestCharge { get; init; }        // RU consumed
}
```

## Recommended Usage Patterns

### 1. Simple Next/Previous Navigation

```csharp
// Perfect for "Load More" buttons or simple navigation
public async Task<NavigationResult> GetNextProducts(string? continuationToken = null)
{
    var result = await cosmosContext.GetPagedList<Product>(
        containerName: "products",
        pageSize: 20,
        continuationToken: continuationToken,
        partitionKey: userPartitionKey
    );
    
    return new NavigationResult
    {
        Items = result.Items,
        NextPageToken = result.ContinuationToken,
        HasMoreResults = result.HasMoreResults,
        RequestCharge = result.RequestCharge
    };
}
```

### 2. Processing All Data Efficiently

```csharp
// Stream through all data without loading everything into memory
public async Task ProcessAllProducts()
{
    string? continuationToken = null;
    
    do
    {
        var pageResult = await cosmosContext.GetPagedList<Product>(
            containerName: "products",
            pageSize: 100,
            continuationToken: continuationToken,
            partitionKey: partitionKey
        );

        // Process each item
        foreach (var product in pageResult.Items)
        {
            await ProcessProduct(product);
        }

        continuationToken = pageResult.ContinuationToken;
        
        // Monitor RU consumption
        Console.WriteLine($"Processed {pageResult.Count} items. RU: {pageResult.RequestCharge}");
        
    } while (pageResult.HasMoreResults);
}
```

### 3. Filtered Pagination (Critical Rule)

```csharp
// ? CORRECT: Always use the same filter with continuation tokens
public async Task<PagedResult<Product>> GetFilteredProducts(
    string category, 
    string? continuationToken = null)
{
    return await cosmosContext.GetPagedList<Product>(
        containerName: "products",
        predicate: p => p.Category == category, // SAME predicate every time
        pageSize: 50,
        continuationToken: continuationToken,
        partitionKey: partitionKey
    );
}

// ? WRONG: Don't change filters when using continuation tokens
// This will cause incorrect results
```

## Performance Best Practices

### 1. Always Provide Partition Keys
```csharp
// ? Good - Single partition query
await cosmosContext.GetPagedList<Product>("products", partitionKey: "tenant123");

// ? Expensive - Cross-partition query
await cosmosContext.GetPagedList<Product>("products", partitionKey: null);
```

### 2. Choose Appropriate Page Sizes
- **Small pages (20-50)**: Better for UI responsiveness
- **Medium pages (100-200)**: Good balance for most scenarios  
- **Large pages (500+)**: Use only for batch processing

### 3. Monitor Request Charges
```csharp
var result = await cosmosContext.GetPagedList<Product>(...);
logger.LogInformation($"Page loaded: {result.Count} items, RU consumed: {result.RequestCharge}");
```

### 4. Use SQL-Based Methods for Complex Queries
When you need complex WHERE clauses, use SQL-based pagination for better continuation token reliability.

## UI Integration Examples

### Web API Controller
```csharp
[HttpGet]
public async Task<ActionResult<PagedResult<Product>>> GetProducts(
    [FromQuery] string? continuationToken = null,
    [FromQuery] int pageSize = 50,
    [FromQuery] string? category = null)
{
    // Limit page size to prevent abuse
    pageSize = Math.Min(pageSize, 100);
    
    PagedResult<Product> result;
    
    if (string.IsNullOrEmpty(category))
    {
        result = await cosmosContext.GetPagedList<Product>(
            "products", pageSize, continuationToken, GetUserPartitionKey());
    }
    else
    {
        result = await cosmosContext.GetPagedList<Product>(
            "products", 
            p => p.Category == category, 
            pageSize, 
            continuationToken, 
            GetUserPartitionKey());
    }
    
    return Ok(result);
}
```

### Frontend Integration
```javascript
// React/TypeScript example
const [products, setProducts] = useState([]);
const [continuationToken, setContinuationToken] = useState(null);
const [hasMore, setHasMore] = useState(true);

const loadNextPage = async () => {
    const response = await fetch(`/api/products?continuationToken=${continuationToken}&pageSize=20`);
    const data = await response.json();
    
    setProducts(prev => [...prev, ...data.items]); // Append to existing
    setContinuationToken(data.continuationToken);
    setHasMore(data.hasMoreResults);
};
```

## Key Limitations & Considerations

1. **Sequential Only**: Continuation tokens only work for next page, not random access
2. **Filter Consistency**: Always use identical filters/predicates with continuation tokens
3. **Token Expiration**: Continuation tokens may expire after some time
4. **LINQ Limitations**: Complex LINQ queries may not support continuation tokens properly

## Migration from Non-Paginated Methods

Replace existing calls for better performance:

```csharp
// ? Old approach - loads everything
var allProducts = await cosmosContext.GetList<Product>("products", partitionKey);

// ? New approach - efficient pagination  
string? token = null;
do {
    var page = await cosmosContext.GetPagedList<Product>("products", 100, token, partitionKey);
    ProcessProducts(page.Items);
    token = page.ContinuationToken;
} while (!string.IsNullOrEmpty(token));
```

This approach provides excellent performance and cost optimization while maintaining simplicity for developers.