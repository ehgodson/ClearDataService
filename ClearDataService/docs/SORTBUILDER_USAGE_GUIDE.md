# SortBuilder Usage Guide

The `SortBuilder<T>` provides a fluent API for building sorting criteria that can be applied to paginated queries in the CosmosDB context. This allows for flexible, conditional, and multi-level sorting.

## Basic Usage

### Simple Ascending Sort
```csharp
var sortBuilder = SortBuilder<Product>
    .New(p => p.Name); // Default is ascending

var results = await cosmosDbContext.GetPagedList<Product>(
    containerName: "products",
    pageSize: 20,
    sortBuilder: sortBuilder
);
```

### Simple Descending Sort
```csharp
var sortBuilder = SortBuilder<Product>
    .New(p => p.Price, SortDirection.Descending);

var results = await cosmosDbContext.GetPagedList<Product>(
    containerName: "products", 
    pageSize: 20,
    sortBuilder: sortBuilder
);
```

## Multiple Sort Criteria

### Multi-Level Sorting
```csharp
var sortBuilder = SortBuilder<Product>
    .Create()
    .ThenBy(p => p.Category)                    // First by category (ascending)
    .ThenByDescending(p => p.Price)             // Then by price (descending)
    .ThenBy(p => p.Name);                       // Finally by name (ascending)

var results = await cosmosDbContext.GetPagedList<Product>(
    containerName: "products",
    pageSize: 20,
    sortBuilder: sortBuilder
);
```

## Conditional Sorting

### Dynamic Sorting Based on Conditions
```csharp
var sortBuilder = SortBuilder<Product>
    .Create()
    .ThenBy(sortByPrice, p => p.Price, direction)
    .ThenBy(sortByDate, p => p.CreatedDate, direction)
    .ThenBy(p => p.Name); // Always sort by name as fallback

var results = await cosmosDbContext.GetPagedList<Product>(
    containerName: "products",
    pageSize: 20,
    sortBuilder: sortBuilder
);
```

## Combining with Filtering

### Filter + Sort
```csharp
// Build filter predicate
var filterBuilder = PredicateBuilder<Product>
    .Create()
    .And(p => p.IsActive)
    .And(minPrice.HasValue, p => p.Price >= minPrice!.Value)
    .And(!string.IsNullOrEmpty(category), p => p.Category == category!);

// Build sort criteria
var sortBuilder = SortBuilder<Product>
    .Create()
    .ThenBy(p => p.Category)
    .ThenByDescending(p => p.Price)
    .ThenBy(p => p.Name);

var results = await cosmosDbContext.GetPagedList<Product>(
    containerName: "products",
    predicate: filterBuilder.Build(),
    pageSize: 20,
    sortBuilder: sortBuilder
);
```

## SQL-Based Pagination with Sorting

### Using SQL Queries with Sorting
```csharp
var sortBuilder = SortBuilder<Product>
    .Create()
    .ThenBy(p => p.Category)
    .ThenByDescending(p => p.Price);

var results = await cosmosDbContext.GetPagedListWithSql<Product>(
    containerName: "products",
    whereClause: "c.data.isActive = true AND c.data.price >= @minPrice",
    parameters: new Dictionary<string, object> { ["minPrice"] = 10.0m },
    pageSize: 20,
    sortBuilder: sortBuilder
);
```

## Working with Documents

### Sorting Cosmos Documents
```csharp
// Sort by entity properties (framework converts to document sorting internally)
var sortBuilder = SortBuilder<Product>
    .Create()
    .ThenBy(p => p.Category)
    .ThenByDescending(p => p.Price);

var documents = await cosmosDbContext.GetPagedDocuments<Product>(
    containerName: "products",
    pageSize: 20,
    sortBuilder: sortBuilder
);
```

## Hierarchical Partition Keys

### Sorting with Hierarchical Partition Keys
```csharp
var hierarchicalKey = new HierarchicalPartitionKey("tenant1", "region1");

var sortBuilder = SortBuilder<Product>
    .Create()
    .ThenByDescending(p => p.CreatedDate)
    .ThenBy(p => p.Name);

var results = await cosmosDbContext.GetPagedList<Product>(
    containerName: "products",
    pageSize: 20,
    hierarchicalPartitionKey: hierarchicalKey,
    sortBuilder: sortBuilder
);
```

## Dynamic Sorting

### Building Sort Criteria Dynamically
```csharp
var sortCriteria = new Dictionary<string, SortDirection>
{
    ["price"] = SortDirection.Descending,
    ["category"] = SortDirection.Ascending,
    ["name"] = SortDirection.Ascending
};

var sortBuilder = SortBuilder<Product>.Create();

foreach (var criteria in sortCriteria)
{
    switch (criteria.Key.ToLower())
    {
        case "name":
            sortBuilder.ThenBy(p => p.Name, criteria.Value);
            break;
        case "price":
            sortBuilder.ThenBy(p => p.Price, criteria.Value);
            break;
        case "category":
            sortBuilder.ThenBy(p => p.Category, criteria.Value);
            break;
    }
}

var results = await cosmosDbContext.GetPagedList<Product>(
    containerName: "products",
    pageSize: 20,
    sortBuilder: sortBuilder
);
```

## No Sorting (Default Behavior)

When no `SortBuilder` is provided, the methods use the natural/default ordering from CosmosDB:

```csharp
// No sorting applied - uses CosmosDB natural ordering
var results = await cosmosDbContext.GetPagedList<Product>(
    containerName: "products",
    pageSize: 20
    // sortBuilder: null (default)
);
```

## Key Features

1. **Fluent API**: Chain multiple sorting criteria with `ThenBy` and `ThenByDescending`
2. **Conditional Sorting**: Add sort criteria based on runtime conditions
3. **Type Safety**: Compile-time checking of property access
4. **Multiple Sort Levels**: Support for complex multi-level sorting
5. **SQL Generation**: Automatically converts to SQL ORDER BY clauses for SQL-based queries
6. **Document Integration**: Seamlessly works with both entities and CosmosDB documents
7. **Null-Safe**: Handles null sort builders gracefully

## Supported Methods

All these methods support the `SortBuilder<T>` parameter:

- `GetPagedList<T>` (all overloads)
- `GetPagedDocuments<T>` (all overloads)
- `GetPagedListWithSql<T>`
- `GetPagedDocumentsWithSql<T>`
- Hierarchical partition key variants of the above methods

The sorting functionality integrates seamlessly with existing filtering, pagination, and partition key features.