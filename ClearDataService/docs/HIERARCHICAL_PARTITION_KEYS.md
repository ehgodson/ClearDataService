# Hierarchical Partition Keys in ClearDataService

## Overview

Hierarchical partition keys allow you to create a multi-level partitioning strategy in Azure Cosmos DB, providing better data distribution and query performance for complex scenarios like multi-tenant applications.

## Benefits

- **Better data distribution** across physical partitions
- **Improved query performance** by targeting specific partition key combinations
- **Multi-tenant isolation** with tenant-level partitioning
- **Flexible data modeling** with multiple partition key levels (up to 3 levels in Cosmos DB)

## Container Creation

### Single Partition Key (Default)

```csharp
// Using default /partitionKey
var container = new CosmosDbContainerInfo("Users");

// Or explicitly
var container = CosmosDbContainerInfo.CreateSingle("Users");

// Implicit conversion
CosmosDbContainerInfo container = "Users";
```

### Hierarchical Partition Keys

```csharp
// Two-level hierarchy: Tenant ? User
var userContainer = CosmosDbContainerInfo.CreateHierarchical(
    "Users",
    "/tenantId",
    "/userId"
);

// Three-level hierarchy: Tenant ? Department ? Employee
var employeeContainer = CosmosDbContainerInfo.CreateHierarchical(
    "Employees",
 "/tenantId",
    "/departmentId",
    "/employeeId"
);

// Multi-tenant helper
var ordersContainer = CosmosDbContainerInfo.CreateMultiTenant(
    "Orders",
    "/tenantId",      // Primary tenant identifier
    "/customerId",// Secondary partition
 "/orderId"      // Tertiary partition
);
```

## Container Setup in Application

```csharp
// Program.cs
var app = builder.Build();

// Create containers with hierarchical partition keys
app.CreateCosmosDatabaseAndContainers(
    // Single partition key
    CosmosDbContainerInfo.CreateSingle("Products"),
    
    // Hierarchical partition keys
    CosmosDbContainerInfo.CreateHierarchical("Users", "/tenantId", "/userId"),
    CosmosDbContainerInfo.CreateHierarchical("Orders", "/tenantId", "/customerId", "/orderId"),
    CosmosDbContainerInfo.CreateMultiTenant("Documents", "/tenantId", "/folderId")
);

app.Run();
```

## Working with Hierarchical Partition Keys

### Creating Entities

```csharp
// Define your entity
public class TenantUser : BaseCosmosDbEntity
{
    public required string TenantId { get; set; }
    public required string UserId { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
}

// Create hierarchical partition key
var partitionKey = HierarchicalPartitionKey.Create(
    "tenant-123",  // Level 1: Tenant
    "user-456"       // Level 2: User
);

// Save entity
var user = new TenantUser
{
    Id = Guid.NewGuid().ToString(),
    TenantId = "tenant-123",
    UserId = "user-456",
    Name = "John Doe",
    Email = "john@example.com"
};

await cosmosContext.Save("Users", user, partitionKey);
```

### Querying with Hierarchical Partition Keys

```csharp
// Query all users for a specific tenant
var tenantKey = HierarchicalPartitionKey.Create("tenant-123");
var tenantUsers = await cosmosContext.GetList<TenantUser>("Users", tenantKey);

// Query specific user
var userKey = HierarchicalPartitionKey.Create("tenant-123", "user-456");
var user = await cosmosContext.Get<TenantUser>("Users", userId, userKey);

// Query with filter
var activeUsers = await cosmosContext.GetList<TenantUser>(
    "Users",
    FilterBuilder<TenantUser>.Create().Where(u => u.IsActive),
    tenantKey
);
```

### Batch Operations with Hierarchical Keys

```csharp
// All items in a batch MUST have the same hierarchical partition key
var partitionKey = HierarchicalPartitionKey.Create("tenant-123", "user-456");

var users = new List<TenantUser>
{
    new() { Id = "1", TenantId = "tenant-123", UserId = "user-456", Name = "User 1" },
    new() { Id = "2", TenantId = "tenant-123", UserId = "user-456", Name = "User 2" },
    new() { Id = "3", TenantId = "tenant-123", UserId = "user-456", Name = "User 3" }
};

foreach (var user in users)
{
    cosmosContext.AddToBatch("Users", user, partitionKey);
}

var results = await cosmosContext.SaveBatchAsync();
```

## Best Practices

### 1. **Choose Partition Keys Carefully**

```csharp
// Good: Even distribution
CosmosDbContainerInfo.CreateHierarchical("Orders", "/tenantId", "/customerId", "/year")

// Bad: Uneven distribution
CosmosDbContainerInfo.CreateHierarchical("Orders", "/country", "/customerId")  // Some countries have way more customers
```

### 2. **Match Document Structure to Partition Key**

```csharp
// Container with /tenantId/userId partition key
public class UserProfile : BaseCosmosDbEntity
{
    // These properties SHOULD match the partition key paths
    public required string TenantId { get; set; }
    public required string UserId { get; set; }
  
    // Other properties
    public string? DisplayName { get; set; }
    public string? Avatar { get; set; }
}
```

### 3. **Use Consistent Partition Key Values**

```csharp
// Good: Consistent values
var partitionKey = HierarchicalPartitionKey.Create("tenant-123", "user-456");
var doc = CosmosDbDocument<TenantUser>.Create(user, partitionKey.ToString());

// Document properties should match partition key values
// user.TenantId should be "tenant-123"
// user.UserId should be "user-456"
```

### 4. **Limit Partition Key Levels**

Azure Cosmos DB supports up to **3 levels** of hierarchical partition keys:

```csharp
// Maximum supported
CosmosDbContainerInfo.CreateHierarchical("Data", "/level1", "/level2", "/level3")

// More than 3 levels will fail
// CosmosDbContainerInfo.CreateHierarchical("Data", "/l1", "/l2", "/l3", "/l4")  // ? Not supported
```

### 5. **Query Performance**

```csharp
// Most efficient: Query with full partition key
var fullKey = HierarchicalPartitionKey.Create("tenant-123", "user-456");
var user = await cosmosContext.Get<User>("Users", userId, fullKey);

// Less efficient: Query with partial partition key
var partialKey = HierarchicalPartitionKey.Create("tenant-123");
var users = await cosmosContext.GetList<User>("Users", partialKey);

// Least efficient: Cross-partition query (no partition key)
var allUsers = await cosmosContext.GetList<User>("Users");  // Scans all partitions
```

## Multi-Tenant Example

### Container Setup

```csharp
// Create multi-tenant containers
app.CreateCosmosDatabaseAndContainers(
    CosmosDbContainerInfo.CreateMultiTenant("Users", "/tenantId", "/userId"),
    CosmosDbContainerInfo.CreateMultiTenant("Documents", "/tenantId", "/folderId"),
    CosmosDbContainerInfo.CreateMultiTenant("Settings", "/tenantId")
);
```

### Entity Definitions

```csharp
public class TenantDocument : BaseCosmosDbAuditEntity
{
    public required string TenantId { get; set; }
  public required string FolderId { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public List<string> Tags { get; set; } = new();
}

public class TenantSettings : BaseCosmosDbEntity
{
    public required string TenantId { get; set; }
    public Dictionary<string, string> Settings { get; set; } = new();
    public string? Theme { get; set; }
    public bool EnableFeatureX { get; set; }
}
```

### Repository Implementation

```csharp
public interface ITenantDocumentRepository : ICosmosDbRepo<TenantDocument>
{
    Task<List<TenantDocument>> GetTenantDocumentsAsync(string tenantId);
    Task<List<TenantDocument>> GetFolderDocumentsAsync(string tenantId, string folderId);
    Task<TenantDocument> CreateDocumentAsync(TenantDocument document, string tenantId, string folderId);
}

public class TenantDocumentRepository : BaseCosmosDbRepo<TenantDocument>, ITenantDocumentRepository
{
    public TenantDocumentRepository(ICosmosDbContext context) : base(context, "Documents") { }

    public async Task<List<TenantDocument>> GetTenantDocumentsAsync(string tenantId)
    {
        var partitionKey = HierarchicalPartitionKey.Create(tenantId);
        return await Get(partitionKey);
    }

    public async Task<List<TenantDocument>> GetFolderDocumentsAsync(string tenantId, string folderId)
    {
  var partitionKey = HierarchicalPartitionKey.Create(tenantId, folderId);
        return await Get(partitionKey);
    }

    public async Task<TenantDocument> CreateDocumentAsync(TenantDocument document, string tenantId, string folderId)
    {
        var partitionKey = HierarchicalPartitionKey.Create(tenantId, folderId);
        return await Create(document, partitionKey);
    }
}
```

## Troubleshooting

### Issue: "Partition key provided doesn't correspond to definition"

**Cause**: The partition key values don't match the container's partition key definition.

**Solution**: Ensure your `HierarchicalPartitionKey` has the same number of levels as the container:

```csharp
// Container created with 2 levels
CosmosDbContainerInfo.CreateHierarchical("Users", "/tenantId", "/userId")

// ? Correct: 2 levels
var key = HierarchicalPartitionKey.Create("tenant-123", "user-456");

// ? Wrong: Only 1 level
var wrongKey = HierarchicalPartitionKey.Create("tenant-123");
```

### Issue: Batch operations fail with hierarchical keys

**Cause**: Items in the batch have different partition key values.

**Solution**: Ensure ALL items in a batch have the EXACT same partition key:

```csharp
var partitionKey = HierarchicalPartitionKey.Create("tenant-123", "user-456");

// All items must have the same partition key
context.AddToBatch("Users", user1, partitionKey);  // ?
context.AddToBatch("Users", user2, partitionKey);  // ?
context.AddToBatch("Users", user3, differentKey);  // ? Will fail
```

### Issue: Cross-partition queries are slow

**Cause**: Queries without partition keys scan all physical partitions.

**Solution**: Always provide a partition key when possible:

```csharp
// ? Slow: Cross-partition query
var allUsers = await context.GetList<User>("Users");

// ? Fast: Partition-scoped query
var tenantKey = HierarchicalPartitionKey.Create("tenant-123");
var tenantUsers = await context.GetList<User>("Users", tenantKey);
```

## Migration from Single to Hierarchical

If you need to migrate from single to hierarchical partition keys:

1. **Create a new container** with hierarchical partition keys
2. **Migrate data** to the new container with proper partition key values
3. **Update application code** to use hierarchical keys
4. **Delete old container** once migration is complete

**Note**: You cannot change the partition key definition of an existing container. You must create a new container.

## Additional Resources

- [Azure Cosmos DB Hierarchical Partition Keys Documentation](https://learn.microsoft.com/en-us/azure/cosmos-db/hierarchical-partition-keys)
- [Partitioning Best Practices](https://learn.microsoft.com/en-us/azure/cosmos-db/partitioning-overview)
- ClearDataService Documentation: `ClearDataService-Documentation.md`
