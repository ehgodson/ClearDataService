# CosmosDbContainerInfo - Type-Safe Hierarchical Partition Keys

## Summary of Final Implementation

### ? Key Improvements

1. **Type-Safe API** - Enforces Azure Cosmos DB's 3-level limit at compile time
2. **Explicit Parameters** - No more misleading `params` arrays
3. **Consistent Primary Path** - All containers use `/partitionKey` as Level 1
4. **Clear Intent** - Method signatures clearly indicate the number of levels

---

## API Design

### Why This Approach?

**Problem with `params`**:
```csharp
// ? Misleading - suggests unlimited levels
CreateHierarchical("Container", "/l1", "/l2", "/l3", "/l4", "/l5")
// Would fail at runtime - Azure Cosmos DB only supports 3 levels
```

**Solution with Explicit Parameters**:
```csharp
// ? Compile-time enforcement
CreateHierarchical("Container", "/l2")     // 2 levels
CreateHierarchical("Container", "/l2", "/l3")      // 3 levels
// Cannot add more - type system prevents it!
```

---

## Complete API Reference

### 1-Level (Single Partition Key)

```csharp
// Constructor
new CosmosDbContainerInfo("Products")

// Factory method
CosmosDbContainerInfo.CreateSingle("Products")

// Result: ["/partitionKey"]
```

### 2-Level Hierarchy

```csharp
// Standard
CosmosDbContainerInfo.CreateHierarchical("Users", "/userId")

// Multi-tenant
CosmosDbContainerInfo.CreateMultiTenant("Users", "/userId")

// Result: ["/partitionKey", "/userId"]
```

### 3-Level Hierarchy (Maximum)

```csharp
// Standard
CosmosDbContainerInfo.CreateHierarchical("Orders", "/customerId", "/orderId")

// Multi-tenant
CosmosDbContainerInfo.CreateMultiTenant("Documents", "/folderId", "/documentId")

// Result: ["/partitionKey", "/customerId", "/orderId"]
```

---

## Validation Rules

The implementation validates:

| Rule | Description | When Checked |
|------|-------------|--------------|
| Non-null | `secondPath` and `thirdPath` cannot be null/empty | Runtime |
| Starts with `/` | All paths must start with forward slash | Runtime |
| Not reserved | Cannot use `/partitionKey` for levels 2-3 | Runtime |
| Max 3 levels | Cannot exceed 3 partition key levels | **Compile-time** |

---

## Migration Guide

### From Old API (Custom Paths)

**Before**:
```csharp
// Different primary paths - inconsistent
var users = new CosmosDbContainerInfo("Users", "/userId");
var orders = new CosmosDbContainerInfo("Orders", "/customerId");
```

**After**:
```csharp
// Standardized primary path, additional levels explicit
var users = CosmosDbContainerInfo.CreateHierarchical("Users", "/userId");
// Structure: ["/partitionKey", "/userId"]

var orders = CosmosDbContainerInfo.CreateHierarchical("Orders", "/customerId", "/orderId");
// Structure: ["/partitionKey", "/customerId", "/orderId"]
```

### Data Migration Required

?? **Important**: You cannot change the partition key of an existing container.

To migrate:
1. Create new containers with the standardized structure
2. Copy data from old containers to new containers
3. Update your code to use new partition key values
4. Delete old containers after verification

---

## Usage Examples by Scenario

### Single-Tenant Application
```csharp
// Simple - just need basic partitioning
var products = new CosmosDbContainerInfo("Products");
var users = new CosmosDbContainerInfo("Users");
```

### Multi-Tenant Application (2-Level)
```csharp
// Tenant + User isolation
var users = CosmosDbContainerInfo.CreateMultiTenant("Users", "/userId");

// Usage
var key = HierarchicalPartitionKey.Create(tenantId, userId);
```

### Complex Multi-Tenant (3-Level)
```csharp
// Tenant + Folder + Document hierarchy
var docs = CosmosDbContainerInfo.CreateMultiTenant("Documents", "/folderId", "/documentId");

// Usage
var key = HierarchicalPartitionKey.Create(tenantId, folderId, documentId);
```

### E-Commerce Application
```csharp
// Products: Single level
var products = CosmosDbContainerInfo.CreateSingle("Products");

// Users: 2 levels
var users = CosmosDbContainerInfo.CreateHierarchical("Users", "/userId");

// Orders: 3 levels for optimal distribution
var orders = CosmosDbContainerInfo.CreateHierarchical("Orders", "/customerId", "/orderId");
```

---

## Benefits of This Design

### 1. Compile-Time Safety
```csharp
// ? Valid - 2 levels
CreateHierarchical("Users", "/userId")

// ? Valid - 3 levels
CreateHierarchical("Orders", "/customerId", "/orderId")

// ? Compiler error - no overload for 4+ levels
CreateHierarchical("Invalid", "/l2", "/l3", "/l4")
```

### 2. Self-Documenting
```csharp
// Method name tells you exactly what you're creating
CreateSingle(name)           // 1 level
CreateHierarchical(name, secondPath)  // 2 levels
CreateHierarchical(name, second, third) // 3 levels
```

### 3. IDE Support
- IntelliSense shows available overloads
- Parameter names indicate purpose (`secondPath`, `thirdPath`)
- No ambiguity about how many levels are supported

### 4. Prevents Runtime Errors
```csharp
// Old way with params - fails at runtime
CreateHierarchical("Bad", "/a", "/b", "/c", "/d")  // Runtime error

// New way - won't compile
CreateHierarchical("Good", "/a", "/b", "/c", "/d")  // Compile error
```

---

## Property Reference

| Property | Type | Description | Example |
|----------|------|-------------|---------|
| `Name` | `string` | Container name | `"Users"` |
| `PartitionKeyPath` | `string` | Primary path (always `/partitionKey`) | `"/partitionKey"` |
| `PartitionKeyPaths` | `string[]` | All partition key paths | `["/partitionKey", "/userId"]` |
| `IsHierarchical` | `bool` | True if 2+ levels | `true` |
| `PartitionKeyLevels` | `int` | Number of levels (1-3) | `2` |

---

## Common Patterns

### Pattern 1: Start Simple, Scale Later
```csharp
// Phase 1: Single partition key
var users = new CosmosDbContainerInfo("Users");

// Phase 2: Add hierarchy when needed (requires new container)
var usersV2 = CosmosDbContainerInfo.CreateHierarchical("UsersV2", "/userId");
```

### Pattern 2: Tenant Isolation
```csharp
// Everything isolated by tenant at Level 1
var users = CosmosDbContainerInfo.CreateMultiTenant("Users", "/userId");
var docs = CosmosDbContainerInfo.CreateMultiTenant("Documents", "/folderId");
var settings = CosmosDbContainerInfo.CreateSingle("Settings");
```

### Pattern 3: Balanced Distribution
```csharp
// Use 3 levels for large datasets with clear hierarchy
var orders = CosmosDbContainerInfo.CreateHierarchical(
  "Orders",
    "/customerId",    // Distribute by customer
    "/orderId"        // Further distribute by order
);
```

---

## Testing Considerations

```csharp
[Fact]
public void Should_Enforce_3Level_Limit_At_CompileTime()
{
    // This test documents that the API prevents >3 levels
    // The fact that this compiles proves the constraint:
    
    var level1 = new CosmosDbContainerInfo("One");
    var level2 = CosmosDbContainerInfo.CreateHierarchical("Two", "/a");
    var level3 = CosmosDbContainerInfo.CreateHierarchical("Three", "/a", "/b");
    
    // No method exists for 4 levels - compiler enforces it
    // var level4 = CosmosDbContainerInfo.CreateHierarchical("Four", "/a", "/b", "/c");
    
    Assert.Equal(1, level1.PartitionKeyLevels);
    Assert.Equal(2, level2.PartitionKeyLevels);
    Assert.Equal(3, level3.PartitionKeyLevels);
}
```

---

## Summary

### Before This Update
- ? Used `params` suggesting unlimited levels
- ? Different containers could have different primary partition key paths
- ? Easy to accidentally exceed 3-level limit
- ? Runtime errors when limits exceeded

### After This Update
- ? Explicit `secondPath` and `thirdPath` parameters
- ? All containers use `/partitionKey` as primary path
- ? Compile-time enforcement of 3-level limit
- ? Type-safe, self-documenting API

---

## Quick Reference

```csharp
// 1 level
new CosmosDbContainerInfo("Container")
CosmosDbContainerInfo.CreateSingle("Container")

// 2 levels
CosmosDbContainerInfo.CreateHierarchical("Container", "/secondPath")
CosmosDbContainerInfo.CreateMultiTenant("Container", "/secondPath")

// 3 levels (maximum)
CosmosDbContainerInfo.CreateHierarchical("Container", "/secondPath", "/thirdPath")
CosmosDbContainerInfo.CreateMultiTenant("Container", "/secondPath", "/thirdPath")
```

---

## Related Documentation

- **Full Guide**: `HIERARCHICAL_PARTITION_KEYS.md`
- **Code Examples**: `Examples/HIERARCHICAL_PARTITION_KEY_EXAMPLES.md`
- **Migration Guide**: `COSMOSDB_CONTAINER_INFO_CHANGES.md`
- **Main Docs**: `ClearDataService-Documentation.md`

---

**Version**: 3.0.2  
**Date**: 2024  
**Azure Cosmos DB Version**: SDK v3 with hierarchical partition key support
