# GetList/GetDocuments Sorting Enhancement

## Summary

Added optional `SortBuilder<T>` parameter to `GetList` and `GetDocuments` methods, completing the consistency pattern across all query methods.

---

## What Changed

### Before
```csharp
// ? Inconsistent - no sorting support
Task<List<T>> GetList<T>(
    string containerName,
    ConsmosDbPartitionKey? partitionKey = null,
    FilterBuilder<T>? filter = null,
    CancellationToken cancellationToken = default
)

Task<List<CosmosDbDocument<T>>> GetDocuments<T>(
    string containerName,
    ConsmosDbPartitionKey? partitionKey = null,
    FilterBuilder<T>? filter = null,
    CancellationToken cancellationToken = default
)
```

### After
```csharp
// ? Consistent - supports filtering AND sorting
Task<List<T>> GetList<T>(
    string containerName,
    ConsmosDbPartitionKey? partitionKey = null,
    FilterBuilder<T>? filter = null,
    SortBuilder<T>? sortBuilder = null,  // ? NEW!
    CancellationToken cancellationToken = default
)

Task<List<CosmosDbDocument<T>>> GetDocuments<T>(
    string containerName,
    ConsmosDbPartitionKey? partitionKey = null,
    FilterBuilder<T>? filter = null,
    SortBuilder<T>? sortBuilder = null,  // ? NEW!
    CancellationToken cancellationToken = default
)
```

---

## Benefits

### 1. **API Consistency**
All list/collection methods now support both filtering AND sorting:

| Method | Filter Support | Sort Support |
|--------|---------------|--------------|
| `GetList<T>` | ? | ? **NEW** |
| `GetDocuments<T>` | ? | ? **NEW** |
| `GetPagedList<T>` | ? | ? |
| `GetPagedDocuments<T>` | ? | ? |

### 2. **Better Developer Experience**
No need to use pagination just to sort results:

```csharp
// ? Before - had to use pagination for sorting
var pagedResult = await context.GetPagedList<Product>(
    "Products",
    pageSize: 1000,  // Large page size just to get sorting
    sortBuilder: mySort
);
var products = pagedResult.Items; // Extract all items

// ? After - direct sorting on GetList
var products = await context.GetList<Product>(
  "Products",
    sortBuilder: mySort  // Much cleaner!
);
```

### 3. **Common Use Cases Enabled**

#### Top N Results
```csharp
// Get top 10 most expensive products
var topProducts = await context.GetList<Product>(
    "Products",
    partitionKey: "electronics",
    filter: FilterBuilder<Product>.Create()
        .And(doc => doc.Data.IsActive),
    sortBuilder: SortBuilder<Product>.Create()
        .ThenByDescending(doc => doc.Data.Price)
);
var top10 = topProducts.Take(10);
```

#### Alphabetical Sorting
```csharp
// Get all users sorted by name
var sortedUsers = await context.GetList<User>(
    "Users",
    partitionKey: tenantId,
    sortBuilder: SortBuilder<User>.Create()
      .ThenBy(doc => doc.Data.Name)
);
```

#### Multi-Level Sorting
```csharp
// Get documents sorted by category, then price
var documents = await context.GetDocuments<Product>(
    "Products",
    sortBuilder: SortBuilder<Product>.Create()
        .ThenBy(doc => doc.Data.Category)
        .ThenByDescending(doc => doc.Data.Price)
        .ThenBy(doc => doc.Data.Name)
);
```

---

## Implementation Details

### GetList Implementation
```csharp
public async Task<List<T>> GetList<T>(
    string containerName,
ConsmosDbPartitionKey? partitionKey = null,
  FilterBuilder<T>? filter = null,
    SortBuilder<T>? sortBuilder = null,
    CancellationToken cancellationToken = default
) where T : ICosmosDbEntity
{
    var query = GetAsQueryable<T>(containerName, partitionKey, filter);

  // Apply sorting if provided
    IQueryable<CosmosDbDocument<T>> orderedQuery = sortBuilder?.HasSortCriteria == true
   ? sortBuilder.ApplyTo(query)
        : query;

    return (await orderedQuery.ToResult(cancellationToken))
   .Select(x => x.Data)
        .ToList();
}
```

### GetDocuments Implementation
```csharp
public async Task<List<CosmosDbDocument<T>>> GetDocuments<T>(
    string containerName,
    ConsmosDbPartitionKey? partitionKey = null,
    FilterBuilder<T>? filter = null,
    SortBuilder<T>? sortBuilder = null,
    CancellationToken cancellationToken = default
) where T : ICosmosDbEntity
{
    var query = GetAsQueryable<T>(containerName, partitionKey, filter);

    // Apply sorting if provided
    IQueryable<CosmosDbDocument<T>> orderedQuery = sortBuilder?.HasSortCriteria == true
 ? sortBuilder.ApplyTo(query)
  : query;

    return await orderedQuery.ToResult(cancellationToken);
}
```

---

## Usage Examples

### Example 1: Simple Sorting
```csharp
// Get all products sorted by name
var products = await context.GetList<Product>(
    "Products",
  sortBuilder: SortBuilder<Product>.Create()
        .ThenBy(doc => doc.Data.Name)
);
```

### Example 2: Filter + Sort
```csharp
// Get active products, sorted by price descending
var expensiveProducts = await context.GetList<Product>(
    "Products",
    filter: FilterBuilder<Product>.Create()
        .And(doc => doc.Data.IsActive),
    sortBuilder: SortBuilder<Product>.Create()
     .ThenByDescending(doc => doc.Data.Price)
);
```

### Example 3: Partition + Filter + Sort
```csharp
// Get electronics products in a category, sorted
var electronics = await context.GetList<Product>(
    "Products",
    partitionKey: "electronics",
    filter: FilterBuilder<Product>.Create()
        .And(doc => doc.Data.Stock > 0),
    sortBuilder: SortBuilder<Product>.Create()
        .ThenBy(doc => doc.Data.Name)
);
```

### Example 4: Documents with Metadata
```csharp
// Get documents sorted by multiple criteria
var documents = await context.GetDocuments<Order>(
    "Orders",
    partitionKey: customerId,
sortBuilder: SortBuilder<Order>.Create()
      .ThenByDescending(doc => doc.Data.OrderDate)
        .ThenBy(doc => doc.Data.TotalAmount)
);

// Access metadata
foreach (var doc in documents)
{
    Console.WriteLine($"Order: {doc.Data.Id}, ETag: {doc.ETag}");
}
```

### Example 5: Conditional Sorting
```csharp
public async Task<List<Product>> GetProducts(
    string sortBy = "name",
    bool descending = false)
{
 var sortBuilder = SortBuilder<Product>.Create();

    var direction = descending 
   ? SortDirection.Descending 
  : SortDirection.Ascending;

    switch (sortBy.ToLower())
 {
     case "name":
            sortBuilder.ThenBy(doc => doc.Data.Name, direction);
         break;
        case "price":
         sortBuilder.ThenBy(doc => doc.Data.Price, direction);
 break;
        case "date":
            sortBuilder.ThenBy(doc => doc.Data.CreatedDate, direction);
            break;
    }

 return await context.GetList<Product>(
        "Products",
        sortBuilder: sortBuilder.HasSortCriteria ? sortBuilder : null
    );
}
```

---

## Breaking Changes

**None!** The `sortBuilder` parameter is optional with a default value of `null`, so all existing code continues to work unchanged:

```csharp
// ? Existing code still works
await context.GetList<Product>("Products");
await context.GetList<Product>("Products", partitionKey: "electronics");
await context.GetList<Product>("Products", filter: myFilter);

// ? New capability available
await context.GetList<Product>("Products", sortBuilder: mySort);
```

---

## Complete Feature Matrix

### Query Methods Comparison

| Method | Pagination | Filter | Sort | Use Case |
|--------|-----------|--------|------|----------|
| `Get<T>` (by ID) | ? | ? | ? | Single item by ID |
| `Get<T>` (by filter) | ? | ? | ? | Single item by criteria |
| `GetList<T>` | ? | ? | ? | All items with filter/sort |
| `GetDocument<T>` (by ID) | ? | ? | ? | Single document by ID |
| `GetDocument<T>` (by filter) | ? | ? | ? | Single document by criteria |
| `GetDocuments<T>` | ? | ? | ? | All documents with filter/sort |
| `GetPagedList<T>` | ? | ? | ? | Paginated items |
| `GetPagedDocuments<T>` | ? | ? | ? | Paginated documents |
| `GetPagedListWithSql<T>` | ? | ? (SQL) | ? | SQL-based pagination |
| `GetPagedDocumentsWithSql<T>` | ? | ? (SQL) | ? | SQL-based doc pagination |

### Pattern Consistency ?

All collection methods now follow the same pattern:
1. ? `partitionKey?` - Optional partition scoping
2. ? `filter?` - Optional filtering
3. ? `sortBuilder?` - Optional sorting
4. ? `cancellationToken` - Cancellation support

---

## Performance Considerations

### Cosmos DB Sorting
- Sorting is performed **server-side** by Cosmos DB (efficient)
- Uses Cosmos DB's indexing for optimal performance
- No client-side sorting overhead

### When to Use GetList vs GetPagedList

#### Use `GetList` When:
- ? Result set is small (< 100 items)
- ? You need all results at once
- ? Partition key is specific (limits results)
- ? Simpler code is preferred

#### Use `GetPagedList` When:
- ? Result set might be large (> 100 items)
- ? You need progressive loading
- ? You want to show "Load More" functionality
- ? You need to control RU consumption

---

## Updated Method Count

**Total Methods: Still 21** (no new methods, just enhanced existing ones)

### Method List
```
PRIVATE HELPER METHODS (2)
??? GetPartitionKey(ConsmosDbPartitionKey?)
??? GetContainer(string)

GET OPERATIONS (3)
??? Get<T>(containerName, id, partitionKey?, cancellationToken)
??? Get<T>(containerName, filter, partitionKey?, cancellationToken)
??? GetList<T>(containerName, partitionKey?, filter?, sortBuilder?, cancellationToken) ? ENHANCED

DOCUMENT OPERATIONS (3)
??? GetDocument<T>(containerName, id, partitionKey?, cancellationToken)
??? GetDocument<T>(containerName, filter, partitionKey?, cancellationToken)
??? GetDocuments<T>(containerName, partitionKey?, filter?, sortBuilder?, cancellationToken) ? ENHANCED

PAGINATION OPERATIONS (2)
??? GetPagedList<T>(containerName, pageSize, continuationToken?, partitionKey?, filter?, sortBuilder?, cancellationToken)
??? GetPagedDocuments<T>(containerName, pageSize, continuationToken?, partitionKey?, filter?, sortBuilder?, cancellationToken)

SQL-BASED PAGINATION (2)
??? GetPagedListWithSql<T>(containerName, whereClause, parameters?, pageSize, continuationToken?, partitionKey?, sortBuilder?, cancellationToken)
??? GetPagedDocumentsWithSql<T>(containerName, whereClause, parameters?, pageSize, continuationToken?, partitionKey?, sortBuilder?, cancellationToken)

QUERYABLE OPERATIONS (1)
??? GetAsQueryable<T>(containerName, partitionKey?, filter?)

SAVE/UPDATE/DELETE OPERATIONS (3)
??? Save<T>(containerName, entity, partitionKey)
??? Upsert<T>(containerName, entity, partitionKey)
??? Delete<T>(containerName, id, partitionKey?)

BATCH OPERATIONS (2)
??? AddToBatch<T>(containerName, item, partitionKey)
??? SaveBatchAsync()

QUERY EXTENSIONS (3)
??? ToResult<T>(query, cancellationToken)
??? ToPagedResult<T>(query, maxItemCount, continuationToken?, cancellationToken)
??? ToPagedResultWithSql<T>(container, queryDefinition, maxItemCount, continuationToken?, partitionKey?, cancellationToken)
```

---

## Summary

| Aspect | Value |
|--------|-------|
| **Methods Enhanced** | 2 (`GetList`, `GetDocuments`) |
| **New Parameters** | `SortBuilder<T>? sortBuilder = null` |
| **Breaking Changes** | 0 - Fully backward compatible |
| **API Consistency** | ? Complete - All list methods support filter + sort |
| **Performance Impact** | ? Positive - Server-side sorting |
| **Developer Experience** | ? Improved - No need for pagination hack |

---

**Version**: 3.0.5+  
**Status**: Production Ready  
**Breaking Changes**: None - Fully backward compatible  
**Feature**: Complete sorting support across all collection methods
