# ClearDataService Documentation

Welcome to the ClearDataService documentation! This guide covers all features, patterns, and best practices for working with Cosmos DB and SQL Server data access.

---

## ?? Table of Contents

### Core Concepts
- [API Simplification Journey](#api-simplification-journey)
- [Type-Safe Partition Keys](#type-safe-partition-keys)
- [Filtering and Sorting](#filtering-and-sorting)

### Feature Guides
- [Hierarchical Partition Keys](#hierarchical-partition-keys)
- [FilterBuilder Usage](#filterbuilder-usage)
- [SortBuilder Usage](#sortbuilder-usage)
- [Pagination](#pagination)

### Migration & Changes
- [CosmosDB Container Info Changes](#cosmosdb-container-info-changes)
- [SortBuilder Refactoring](#sortbuilder-refactoring)

---

## API Simplification Journey

### Phase 1: Unified Partition Keys (50% Reduction)
**Document**: [COSMOSDBCONTEXT_SIMPLIFICATION.md](COSMOSDBCONTEXT_SIMPLIFICATION.md)

Eliminated 21 duplicate methods by introducing `ConsmosDbPartitionKey` with implicit string conversion:
- **Before**: 42 methods (21 with string, 21 with HierarchicalPartitionKey)
- **After**: 21 methods (unified with ConsmosDbPartitionKey)
- **Reduction**: 50%

### Phase 2: Optional Filter/Sort Parameters (71% Total Reduction)
**Document**: [FINAL_API_SIMPLIFICATION.md](FINAL_API_SIMPLIFICATION.md)

Consolidated methods by making filter and sort parameters optional:
- **Before**: 21 methods (separate overloads for filter/no-filter)
- **After**: 12 methods (filter and sort are optional parameters)
- **Total Reduction**: 71% from original 42 methods

### Phase 3: GetList/GetDocuments Sorting Enhancement
**Document**: [GETLIST_SORTING_ENHANCEMENT.md](GETLIST_SORTING_ENHANCEMENT.md)

Added sorting support to `GetList` and `GetDocuments` for API consistency:
- Enhanced 2 methods with optional `SortBuilder<T>` parameter
- All collection methods now support filtering AND sorting
- No breaking changes - fully backward compatible

---

## Type-Safe Partition Keys

### Comprehensive Guide
**Document**: [TYPE_SAFE_PARTITION_KEYS.md](TYPE_SAFE_PARTITION_KEYS.md)

Complete guide to the unified partition key system with implicit string conversion.

### Hierarchical Partition Keys
**Document**: [HIERARCHICAL_PARTITION_KEYS.md](HIERARCHICAL_PARTITION_KEYS.md)

Azure Cosmos DB supports up to 3 levels of hierarchical partition keys for better data distribution:

```csharp
// Single level
var pk = ConsmosDbPartitionKey.Create("tenant-123");

// Two levels
var pk = ConsmosDbPartitionKey.Create("tenant-123", "user-456");

// Three levels (maximum)
var pk = ConsmosDbPartitionKey.Create("tenant-123", "user-456", "doc-789");

// Implicit string conversion
await context.Save(containerName, entity, "tenant-123"); // Still works!
```

**Also see**: [HIERARCHICAL_PARTITION_KEY_EXAMPLES.md](HIERARCHICAL_PARTITION_KEY_EXAMPLES.md)

### Fluent API Guide
**Documents**: 
- [FLUENT_API_GUIDE.md](FLUENT_API_GUIDE.md)
- [HIERARCHICAL_PARTITION_KEY_FLUENT_API.md](HIERARCHICAL_PARTITION_KEY_FLUENT_API.md)

Alternative fluent syntax for building partition keys:

```csharp
var pk = ConsmosDbPartitionKey
    .WithLevel1("tenant-123")
    .AddLevel2("user-456")
    .AddLevel3("doc-789");
```

---

## Filtering and Sorting

### FilterBuilder
**Document**: [FilterBuilder.cs](../Utils/FilterBuilder.cs)

Type-safe filtering for Cosmos DB queries:

```csharp
var filter = FilterBuilder<Product>.Create()
    .And(doc => doc.Data.IsActive)
    .And(doc => doc.Data.Price > 10)
    .And(doc => doc.Data.Category == "Electronics");

var products = await context.GetList<Product>(
 "Products",
    filter: filter
);
```

### SortBuilder
**Documents**:
- [SortBuilder.cs](../Utils/SortBuilder.cs)
- [SORTBUILDER_REFACTORING.md](SORTBUILDER_REFACTORING.md)
- [SortBuilderExamples.cs](../Examples/SortBuilderExamples.cs)

Type-safe sorting for Cosmos DB queries:

```csharp
var sort = SortBuilder<Product>.Create()
    .ThenBy(doc => doc.Data.Category)
    .ThenByDescending(doc => doc.Data.Price)
    .ThenBy(doc => doc.Data.Name);

var products = await context.GetList<Product>(
  "Products",
    sortBuilder: sort
);
```

**Important**: After refactoring, `SortBuilder` works directly with `CosmosDbDocument<T>` (matches `FilterBuilder` pattern):
- Use `doc.Data.PropertyName` syntax
- No runtime conversion overhead
- Consistent API across both builders

---

## Pagination

### Basic Pagination
```csharp
var pagedResult = await context.GetPagedList<Product>(
    "Products",
    pageSize: 50,
    continuationToken: null // First page
);

// Next page
var nextPage = await context.GetPagedList<Product>(
    "Products",
    pageSize: 50,
    continuationToken: pagedResult.ContinuationToken
);
```

### Pagination with Filter and Sort
```csharp
var filter = FilterBuilder<Product>.Create()
    .And(doc => doc.Data.IsActive);

var sort = SortBuilder<Product>.Create()
    .ThenByDescending(doc => doc.Data.CreatedDate);

var pagedResult = await context.GetPagedList<Product>(
    "Products",
    pageSize: 50,
filter: filter,
    sortBuilder: sort
);
```

### SQL-Based Pagination
```csharp
var pagedResult = await context.GetPagedListWithSql<Product>(
    "Products",
    whereClause: "c.data.price > @minPrice",
    parameters: new Dictionary<string, object> { { "minPrice", 100 } },
    pageSize: 50,
    sortBuilder: sort
);
```

---

## Migration & Breaking Changes

### CosmosDB Container Info Changes
**Document**: [COSMOSDB_CONTAINER_INFO_CHANGES.md](COSMOSDB_CONTAINER_INFO_CHANGES.md)

Changes to container configuration and partition key setup.

### SortBuilder Refactoring
**Document**: [SORTBUILDER_REFACTORING.md](SORTBUILDER_REFACTORING.md)

**Breaking Change**: `SortBuilder` now uses `doc.Data.PropertyName` syntax (matches `FilterBuilder`):

**Before**:
```csharp
var sort = SortBuilder<Product>.Create()
    .ThenBy(p => p.Name); // Direct property access
```

**After**:
```csharp
var sort = SortBuilder<Product>.Create()
    .ThenBy(doc => doc.Data.Name); // Document property access
```

**Benefits**:
- Consistency with FilterBuilder
- No runtime conversion overhead
- Eliminated 50+ lines of conversion code

---

## API Reference

### Final Method Count

**Total: 21 methods** (71% reduction from original 42)

```
PRIVATE HELPER METHODS (2)
??? GetPartitionKey(ConsmosDbPartitionKey?)
??? GetContainer(string)

GET OPERATIONS (3)
??? Get<T>(containerName, id, partitionKey?, cancellationToken)
??? Get<T>(containerName, filter, partitionKey?, cancellationToken)
??? GetList<T>(containerName, partitionKey?, filter?, sortBuilder?, cancellationToken) ?

DOCUMENT OPERATIONS (3)
??? GetDocument<T>(containerName, id, partitionKey?, cancellationToken)
??? GetDocument<T>(containerName, filter, partitionKey?, cancellationToken)
??? GetDocuments<T>(containerName, partitionKey?, filter?, sortBuilder?, cancellationToken) ?

PAGINATION OPERATIONS (2)
??? GetPagedList<T>(..., filter?, sortBuilder?, ...)
??? GetPagedDocuments<T>(..., filter?, sortBuilder?, ...)

SQL-BASED PAGINATION (2)
??? GetPagedListWithSql<T>(..., sortBuilder?, ...)
??? GetPagedDocumentsWithSql<T>(..., sortBuilder?, ...)

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

? = Enhanced with sorting support

---

## Code Examples

### Complete Example Files

- **SortBuilder Examples**: [SortBuilderExamples.cs](../Examples/SortBuilderExamples.cs)
- **GetList Sorting Examples**: [GetListSortingExamples.cs](../Examples/GetListSortingExamples.cs)
- **Hierarchical Partition Key Examples**: [HIERARCHICAL_PARTITION_KEY_EXAMPLES.md](HIERARCHICAL_PARTITION_KEY_EXAMPLES.md)

### Quick Start

```csharp
// Setup
var context = serviceProvider.GetRequiredService<ICosmosDbContext>();

// Save with partition key
await context.Save("Products", product, "tenant-123");

// Get by ID
var product = await context.Get<Product>(
    "Products", 
    "product-id", 
    "tenant-123"
);

// Get list with filter and sort
var filter = FilterBuilder<Product>.Create()
    .And(doc => doc.Data.IsActive)
    .And(doc => doc.Data.Price > 10);

var sort = SortBuilder<Product>.Create()
    .ThenByDescending(doc => doc.Data.Price);

var products = await context.GetList<Product>(
    "Products",
    partitionKey: "tenant-123",
    filter: filter,
    sortBuilder: sort
);

// Pagination
var pagedResult = await context.GetPagedList<Product>(
    "Products",
    pageSize: 50,
    partitionKey: "tenant-123",
    filter: filter,
sortBuilder: sort
);
```

---

## Version History

| Version | Changes | Documents |
|---------|---------|-----------|
| **3.0.5** | GetList/GetDocuments sorting enhancement | [GETLIST_SORTING_ENHANCEMENT.md](GETLIST_SORTING_ENHANCEMENT.md) |
| **3.0.4** | SortBuilder refactoring (doc.Data pattern) | [SORTBUILDER_REFACTORING.md](SORTBUILDER_REFACTORING.md) |
| **3.0.3** | Optional filter/sort parameters (71% reduction) | [FINAL_API_SIMPLIFICATION.md](FINAL_API_SIMPLIFICATION.md) |
| **3.0.2** | Unified partition keys (50% reduction) | [COSMOSDBCONTEXT_SIMPLIFICATION.md](COSMOSDBCONTEXT_SIMPLIFICATION.md) |
| **3.0.1** | Type-safe partition keys | [TYPE_SAFE_PARTITION_KEYS.md](TYPE_SAFE_PARTITION_KEYS.md) |
| **3.0.0** | Hierarchical partition keys | [HIERARCHICAL_PARTITION_KEYS.md](HIERARCHICAL_PARTITION_KEYS.md) |

---

## Performance Tips

### Partition Key Strategy
- Use specific partition keys to limit query scope
- Hierarchical keys provide better data distribution
- Avoid cross-partition queries when possible

### Filtering
- Push filters to the database with `FilterBuilder`
- Avoid client-side filtering with LINQ after `.ToResult()`

### Sorting
- Use `SortBuilder` for server-side sorting
- Combine with pagination for large result sets

### Pagination
- Always use pagination for large datasets
- Set appropriate page sizes (50-100 items)
- Use continuation tokens for efficient paging

---

## Support

For issues, questions, or contributions:
- **GitHub**: [https://github.com/ehgodson/ClearDataService](https://github.com/ehgodson/ClearDataService)
- **Documentation**: This docs folder
- **Examples**: [Examples folder](../Examples/)

---

**Last Updated**: Version 3.0.5+  
**Status**: Production Ready  
**API Stability**: Stable - No breaking changes planned
