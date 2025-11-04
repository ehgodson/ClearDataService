# Sort Builder Refactoring: Alignment with FilterBuilder

## Summary

**SortBuilder** has been refactored to work directly with `CosmosDbDocument<T>` expressions, matching the pattern used by `FilterBuilder<T>`. This eliminates the need for manual conversion logic in `CosmosDbContext` and provides a more consistent API.

---

## Before: Inconsistent Patterns

### FilterBuilder (Correct Pattern)
```csharp
// ? Works with CosmosDbDocument<T> directly
public class FilterBuilder<T> where T : ICosmosDbEntity
{
    private Expression<Func<CosmosDbDocument<T>, bool>>? _predicate;
    
    public FilterBuilder<T> And(Expression<Func<CosmosDbDocument<T>, bool>> predicate) { ... }
}

// Usage
var filter = FilterBuilder<User>.Create()
    .And(doc => doc.Data.IsActive);  // doc.Data.PropertyName
```

### SortBuilder (Old - Inconsistent)
```csharp
// ? Worked with entity T directly (not CosmosDbDocument<T>)
public class SortBuilder<T>
{
 private readonly List<SortCriteria<T>> _sortCriteria;
    
    public SortBuilder<T> ThenBy(Expression<Func<T, object>> keySelector) { ... }
}

// Usage - looked simpler but required conversion!
var sort = SortBuilder<User>.Create()
    .ThenBy(u => u.Name);  // Direct property access

// CosmosDbContext had to convert:
private static SortBuilder<CosmosDbDocument<T>>? ConvertSortBuilderToDocument<T>(
    SortBuilder<T>? entitySortBuilder) 
{
    // 30+ lines of complex conversion logic!
    var documentParam = Expression.Parameter(typeof(CosmosDbDocument<T>), "doc");
    var dataProperty = Expression.Property(documentParam, nameof(CosmosDbDocument<T>.Data));
    // ... more conversion code
}
```

---

## After: Consistent Pattern

### SortBuilder (New - Matches FilterBuilder)
```csharp
// ? Now works with CosmosDbDocument<T> directly
public class SortBuilder<T> where T : ICosmosDbEntity
{
    private readonly List<SortCriteria<T>> _sortCriteria;
    
    public SortBuilder<T> ThenBy(Expression<Func<CosmosDbDocument<T>, object>> keySelector) { ... }
}

// Usage - same pattern as FilterBuilder
var sort = SortBuilder<User>.Create()
    .ThenBy(doc => doc.Data.Name);  // doc.Data.PropertyName
```

### CosmosDbContext (Simplified)
```csharp
// ? No conversion needed!
public async Task<PagedResult<T>> GetPagedList<T>(
string containerName,
    int pageSize = 100,
string? continuationToken = null,
  HierarchicalPartitionKey? partitionKey = null,
    FilterBuilder<T>? filter = null,
    SortBuilder<T>? sortBuilder = null,  // Works directly!
    CancellationToken cancellationToken = default
) where T : ICosmosDbEntity
{
    var query = GetAsQueryable<T>(containerName, partitionKey, filter);

    // ? Direct application - no conversion!
    var sortedQuery = sortBuilder?.HasSortCriteria == true 
  ? sortBuilder.ApplyTo(query)
        : query.OrderBy(x => 1);

    var pagedResult = await sortedQuery.ToPagedResult(pageSize, continuationToken, cancellationToken);
    // ...
}
```

---

## Benefits

### 1. Consistency
- **Before**: `FilterBuilder` used `doc.Data.Prop`, `SortBuilder` used `entity.Prop`
- **After**: Both use `doc.Data.Prop` - consistent API!

### 2. Code Reduction
- **Removed**: 3 helper methods (~50 lines)
  - `ConvertSortBuilderToDocument<T>` (30 lines)
  - `ApplySorting<T>` (8 lines)
  - `GetPropertyNameFromExpression<T>` (8 lines)
- **Simplified**: Pagination methods (each reduced by 5 lines)

### 3. Better Performance
- **Before**: Runtime expression tree conversion for every sorted query
- **After**: Expressions built correctly from the start - no conversion overhead

### 4. Type Safety
- **Same**: Both approaches are type-safe
- **Better**: Clearer what's happening - no hidden conversions

### 5. Easier to Understand
```csharp
// ? Clear and explicit
var sort = SortBuilder<User>.Create()
    .ThenBy(doc => doc.Data.Name)
    .ThenByDescending(doc => doc.Data.CreatedDate);

// You can see you're working with CosmosDbDocument<User>
```

---

## Usage Comparison

### Filtering and Sorting Together

**Before**:
```csharp
// Inconsistent - filter uses doc.Data, sort uses direct properties
var filter = FilterBuilder<Product>.Create()
    .And(doc => doc.Data.IsActive)
    .And(doc => doc.Data.Price > 10);

var sort = SortBuilder<Product>.Create()
    .ThenBy(p => p.Category)      // ? Inconsistent!
    .ThenByDescending(p => p.Price);
```

**After**:
```csharp
// ? Consistent - both use doc.Data pattern
var filter = FilterBuilder<Product>.Create()
    .And(doc => doc.Data.IsActive)
    .And(doc => doc.Data.Price > 10);

var sort = SortBuilder<Product>.Create()
    .ThenBy(doc => doc.Data.Category)      // ? Consistent!
    .ThenByDescending(doc => doc.Data.Price);
```

### Complete Example

```csharp
// Get active products, sorted by category then price
var filter = FilterBuilder<Product>.Create()
    .And(doc => doc.Data.IsActive);

var sort = SortBuilder<Product>.Create()
    .ThenBy(doc => doc.Data.Category)
    .ThenByDescending(doc => doc.Data.Price);

var pagedProducts = await context.GetPagedList<Product>(
    "Products",
    pageSize: 50,
    filter: filter,
    sortBuilder: sort
);
```

---

## Migration Guide

### Updating Existing Code

**Old Code**:
```csharp
var sort = SortBuilder<User>.Create()
    .ThenBy(u => u.Name)
    .ThenByDescending(u => u.CreatedDate);
```

**New Code**:
```csharp
var sort = SortBuilder<User>.Create()
    .ThenBy(doc => doc.Data.Name)
    .ThenByDescending(doc => doc.Data.CreatedDate);
```

**Find and Replace Pattern**:
- Old: `u => u.PropertyName`
- New: `doc => doc.Data.PropertyName`

### Why `doc.Data.PropertyName`?

The Cosmos DB document structure wraps your entity:
```csharp
public class CosmosDbDocument<T>
{
    public string Id { get; set; }
    public string PartitionKey { get; set; }
    public string EntityType { get; set; }
    public T Data { get; set; }  // ? Your entity is here!
    public string ETag { get; set; }
    public DateTime Timestamp { get; set; }
    // ...
}
```

So when sorting/filtering, you access:
- `doc.Id` - Document ID
- `doc.PartitionKey` - Partition key
- `doc.Data` - Your actual entity
- `doc.Data.Name` - Property of your entity

---

## Technical Details

### SortCriteria Update

**Before**:
```csharp
public class SortCriteria<T>
{
public Expression<Func<T, object>> KeySelector { get; }
    public SortDirection Direction { get; }
}
```

**After**:
```csharp
public class SortCriteria<T> where T : ICosmosDbEntity
{
    public Expression<Func<CosmosDbDocument<T>, object>> KeySelector { get; }
    public SortDirection Direction { get; }
}
```

### ApplyTo Method Update

**Before**:
```csharp
public IOrderedQueryable<T> ApplyTo(IQueryable<T> query)
{
    // Applied to entity queryable
    return query.OrderBy(criteria.KeySelector);
}
```

**After**:
```csharp
public IOrderedQueryable<CosmosDbDocument<T>> ApplyTo(IQueryable<CosmosDbDocument<T>> query)
{
    // Applies directly to document queryable
    return query.OrderBy(criteria.KeySelector);
}
```

### SQL ORDER BY Generation

The `ToSqlOrderBy` method was updated to extract property names from `doc.Data.PropertyName` expressions:

```csharp
private static string GetPropertyName(Expression<Func<CosmosDbDocument<T>, object>> expression)
{
    return expression.Body switch
    {
      // Direct member access: doc.Data.PropertyName
        MemberExpression memberExpr when memberExpr.Expression is MemberExpression parentMember 
     && parentMember.Member.Name == "Data" 
          => memberExpr.Member.Name,

        // Unary conversion: Convert(doc.Data.PropertyName)
        UnaryExpression unaryExpr when unaryExpr.Operand is MemberExpression memberExpr2 
&& memberExpr2.Expression is MemberExpression parentMember2 
    && parentMember2.Member.Name == "Data" 
  => memberExpr2.Member.Name,
    
     // ... fallback cases
    };
}
```

---

## Breaking Changes

### None for End Users

If you were using `CosmosDbContext.GetPagedList<T>()` methods:
- **No changes needed** - just update your `SortBuilder` usage

### For Direct SortBuilder Users

If you created `SortBuilder` instances directly:
- **Update syntax**: Change `entity => entity.Prop` to `doc => doc.Data.Prop`
- **Compile-time error**: Old syntax won't compile - easy to find and fix

---

## Summary Statistics

| Aspect | Before | After | Improvement |
|--------|--------|-------|-------------|
| Pattern Consistency | ? Inconsistent | ? Consistent | Matches FilterBuilder |
| Helper Methods | 3 | 0 | **100% eliminated** |
| Lines of Code | ~550 | ~500 | **~10% reduction** |
| Runtime Conversion | Yes | No | **Eliminated overhead** |
| API Clarity | Good | **Better** | Same pattern as FilterBuilder |
| Type Safety | ? | ? | Maintained |

---

**Version**: 3.0.4+  
**Status**: Production Ready  
**Breaking Changes**: Syntax update required for SortBuilder usage  
**Performance Impact**: Positive - eliminated runtime conversion
