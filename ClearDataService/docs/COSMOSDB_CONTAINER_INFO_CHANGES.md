# CosmosDbContainerInfo - Hierarchical Partition Key Support

## Summary of Changes

### Updated `CosmosDbContainerInfo.cs`
- ? Standardized on `/partitionKey` as the **primary** partition key path (Level 1)
- ? Added support for hierarchical partition keys with **explicit** 2nd and 3rd level parameters
- ? **Enforces Azure Cosmos DB's 3-level maximum** through type-safe API
- ? Added `PartitionKeyPaths` property (array) to support multiple paths
- ? Added `IsHierarchical` property to indicate hierarchical partition key usage
- ? Added `PartitionKeyLevels` property to get the number of levels (1-3)
- ? Added factory methods with explicit parameters:
  - `CreateSingle(name)` - Single partition key container (1 level)
  - `CreateHierarchical(name, secondPath)` - 2-level hierarchy
  - `CreateHierarchical(name, secondPath, thirdPath)` - 3-level hierarchy (max)
  - `CreateMultiTenant(name, subPath)` - Multi-tenant with 2 levels
  - `CreateMultiTenant(name, secondPath, thirdPath)` - Multi-tenant with 3 levels

### Updated `CosmosDbMigrator.cs`
- ? Added support for creating containers with hierarchical partition keys
- ? Uses `PartitionKeyDefinitionVersion.V2` for hierarchical keys
- ? Maintains backward compatibility with single partition key containers

## API Design Philosophy

### Why Explicit Parameters Instead of `params`?

**Azure Cosmos DB Limitation**: Maximum 3 hierarchical partition key levels

**Before (Misleading)**:
```csharp
// params suggests unlimited levels - WRONG!
CreateHierarchical("Orders", "/level1", "/level2", "/level3", "/level4", "/level5")  // Would fail at runtime
```

**After (Type-Safe)**:
```csharp
// API enforces the 3-level limit at compile time
CreateHierarchical("Orders", "/customerId")  // 2 levels: /partitionKey + /customerId
CreateHierarchical("Orders", "/customerId", "/orderId")  // 3 levels: /partitionKey + /customerId + /orderId
// Cannot add more - compiler prevents it!
```

## Partition Key Structure

### Level 1 (Primary): Always `/partitionKey`
- This is the **root** partition key for all containers
- Stores the primary partitioning value (e.g., tenantId, categoryId, etc.)
- **Cannot be customized** - ensures consistency across all containers

### Level 2 (Optional): Your `secondPath`
- Additional sub-partitioning within Level 1
- Example: `/userId`, `/customerId`, `/folderId`

### Level 3 (Optional): Your `thirdPath`
- Further sub-partitioning within Level 2
- Example: `/orderId`, `/documentId`, `/itemId`

## API Examples

### Single Partition Key (1 Level)
```csharp
// Only /partitionKey
var products = new CosmosDbContainerInfo("Products");
// or
var products = CosmosDbContainerInfo.CreateSingle("Products");

// Partition key structure: ["/partitionKey"]
```

### Two-Level Hierarchy
```csharp
// /partitionKey + /userId
var users = CosmosDbContainerInfo.CreateHierarchical("Users", "/userId");

// Partition key structure: ["/partitionKey", "/userId"]
// Usage: partitionKey = "tenant-123", userId = "user-456"
```

### Three-Level Hierarchy (Maximum)
```csharp
// /partitionKey + /customerId + /orderId
var orders = CosmosDbContainerInfo.CreateHierarchical("Orders", "/customerId", "/orderId");

// Partition key structure: ["/partitionKey", "/customerId", "/orderId"]
// Usage: partitionKey = "tenant-123", customerId = "cust-789", orderId = "order-001"
```

### Multi-Tenant Scenarios

**2-Level Multi-Tenant**:
```csharp
// /partitionKey (tenantId) + /userId
var users = CosmosDbContainerInfo.CreateMultiTenant("Users", "/userId");

// Partition key structure: ["/partitionKey", "/userId"]
// Usage: Store tenantId in /partitionKey, userId in /userId
```

**3-Level Multi-Tenant**:
```csharp
// /partitionKey (tenantId) + /folderId + /documentId
var documents = CosmosDbContainerInfo.CreateMultiTenant("Documents", "/folderId", "/documentId");

// Partition key structure: ["/partitionKey", "/folderId", "/documentId"]
// Usage: Store tenantId in /partitionKey, folderId in /folderId, documentId in /documentId
```

## Container Setup
```csharp
// Program.cs - All possible configurations
app.CreateCosmosDatabaseAndContainers(
    // 1 level (single partition key)
    new CosmosDbContainerInfo("Products"),
    
    // 2 levels: /partitionKey + /userId
    CosmosDbContainerInfo.CreateHierarchical("Users", "/userId"),
    
    // 3 levels: /partitionKey + /customerId + /orderId
    CosmosDbContainerInfo.CreateHierarchical("Orders", "/customerId", "/orderId"),
    
    // Multi-tenant 2 levels
    CosmosDbContainerInfo.CreateMultiTenant("Settings", "/userId"),
    
    // Multi-tenant 3 levels
    CosmosDbContainerInfo.CreateMultiTenant("Documents", "/folderId", "/documentId")
);
```

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Container name |
| `PartitionKeyPaths` | `string[]` | Array of partition key paths (1-3 elements) |
| `PartitionKeyPath` | `string` | Primary partition key path (always `/partitionKey`) |
| `IsHierarchical` | `bool` | True if using 2 or 3 levels |
| `PartitionKeyLevels` | `int` | Number of partition key levels (1, 2, or 3) |

## Validation Rules

The implementation enforces:
- ? `secondPath` and `thirdPath` cannot be null/empty (when provided)
- ? All paths must start with `/`
- ? Cannot use `/partitionKey` as `secondPath` or `thirdPath` (reserved for Level 1)
- ? Maximum 3 levels enforced by method signatures (compile-time safety)

## Working with Hierarchical Keys

### Creating Hierarchical Partition Keys

```csharp
// 2-level key
var key = HierarchicalPartitionKey.Create("tenant-123", "user-456");

// 3-level key
var key = HierarchicalPartitionKey.Create("tenant-123", "customer-789", "order-001");
```

### Saving Data
```csharp
// For 2-level container (/partitionKey + /userId)
var user = new User 
{ 
    Id = "user-123",
    TenantId = "tenant-456",  // Stored at /partitionKey level
    UserId = "user-789"        // Stored at /userId level
};

var key = HierarchicalPartitionKey.Create("tenant-456", "user-789");
await cosmosContext.Save("Users", user, key);
```

### Querying Data
```csharp
// Query all users for a tenant (partial key - Level 1 only)
var tenantKey = HierarchicalPartitionKey.Create("tenant-123");
var allUsers = await cosmosContext.GetList<User>("Users", tenantKey);

// Query specific user (full key - Levels 1 + 2)
var userKey = HierarchicalPartitionKey.Create("tenant-123", "user-456");
var user = await cosmosContext.Get<User>("Users", userId, userKey);
```

## Comparison: Before vs After

### Before (v3.0.1 and earlier)
```csharp
// Could specify any partition key path - inconsistent
var users = new CosmosDbContainerInfo("Users", "/userId");
var orders = new CosmosDbContainerInfo("Orders", "/customerId");
var products = new CosmosDbContainerInfo("Products", "/productCategory");

// Each container had different primary partition key path - confusing!
```

### After (Current - Type-Safe)
```csharp
// All containers use /partitionKey as primary (Level 1)
// Additional levels are explicit and controlled

var users = CosmosDbContainerInfo.CreateHierarchical("Users", "/userId");
// Structure: ["/partitionKey", "/userId"]

var orders = CosmosDbContainerInfo.CreateHierarchical("Orders", "/customerId", "/orderId");
// Structure: ["/partitionKey", "/customerId", "/orderId"]

var products = new CosmosDbContainerInfo("Products");
// Structure: ["/partitionKey"]

// Consistent primary partition key across all containers!
```

## Benefits

1. **Type Safety**: Cannot exceed 3 levels - enforced at compile time
2. **Consistency**: All containers use `/partitionKey` as the primary path
3. **Clarity**: Method signatures make the level structure explicit
4. **Validation**: Runtime checks ensure valid partition key paths
5. **Documentation**: Self-documenting API - method names indicate level count

## Migration Notes

If you have existing containers with custom partition key paths, you'll need to:

1. **Create new containers** with the standardized `/partitionKey` structure
2. **Migrate your data** to the new containers
3. **Update your code** to use the new API
4. **Delete old containers** once migration is verified

**Note**: You cannot change the partition key definition of an existing container.

## Common Patterns

### Pattern 1: Simple Single-Tenant Application
```csharp
// Just use single partition key
var container = new CosmosDbContainerInfo("Users");
// or
var container = CosmosDbContainerInfo.CreateSingle("Users");
```

### Pattern 2: Multi-Tenant with User Isolation
```csharp
// 2 levels: tenant + user
var container = CosmosDbContainerInfo.CreateMultiTenant("Users", "/userId");

// Usage
var key = HierarchicalPartitionKey.Create(tenantId, userId);
```

### Pattern 3: Complex Multi-Tenant with Document Hierarchy
```csharp
// 3 levels: tenant + folder + document
var container = CosmosDbContainerInfo.CreateMultiTenant("Documents", "/folderId", "/documentId");

// Usage
var key = HierarchicalPartitionKey.Create(tenantId, folderId, documentId);
```

### Pattern 4: E-Commerce with Order Hierarchy
```csharp
// 3 levels: tenant + customer + order
var container = CosmosDbContainerInfo.CreateHierarchical("Orders", "/customerId", "/orderId");

// Usage
var key = HierarchicalPartitionKey.Create(tenantId, customerId, orderId);
```

## Documentation

- **Comprehensive Guide**: `HIERARCHICAL_PARTITION_KEYS.md`
- **Code Examples**: `Examples/HIERARCHICAL_PARTITION_KEY_EXAMPLES.md`
- **Main Documentation**: `ClearDataService-Documentation.md`

## Questions?

Refer to:
- `HIERARCHICAL_PARTITION_KEYS.md` - Comprehensive guide with best practices
- `Examples/HIERARCHICAL_PARTITION_KEY_EXAMPLES.md` - Code examples
- [Azure Cosmos DB Documentation](https://learn.microsoft.com/en-us/azure/cosmos-db/hierarchical-partition-keys)
