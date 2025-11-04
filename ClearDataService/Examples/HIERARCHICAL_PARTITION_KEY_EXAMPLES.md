# Hierarchical Partition Keys - Code Examples

This document contains code examples for using hierarchical partition keys in ClearDataService.

**Important**: Azure Cosmos DB supports a **maximum of 3 hierarchical partition key levels**.  
The API enforces this limit through explicit method signatures.

## Partition Key Structure

- **Level 1 (Primary)**: Always `/partitionKey` - stores the primary partition value (e.g., tenantId)
- **Level 2 (Optional)**: Your `secondPath` - additional sub-partitioning (e.g., `/userId`)
- **Level 3 (Optional)**: Your `thirdPath` - further sub-partitioning (e.g., `/documentId`)

## 1. Container Creation

```csharp
using Clear.DataService;
using Clear.DataService.Models;

// ===== SINGLE PARTITION KEY (1 Level) =====
var products = new CosmosDbContainerInfo("Products");
// Structure: ["/partitionKey"]

// Or explicitly
var products = CosmosDbContainerInfo.CreateSingle("Products");

// ===== TWO-LEVEL HIERARCHY =====
// /partitionKey + /userId
var users = CosmosDbContainerInfo.CreateHierarchical("Users", "/userId");
// Structure: ["/partitionKey", "/userId"]

// ===== THREE-LEVEL HIERARCHY (MAXIMUM) =====
// /partitionKey + /customerId + /orderId
var orders = CosmosDbContainerInfo.CreateHierarchical("Orders", "/customerId", "/orderId");
// Structure: ["/partitionKey", "/customerId", "/orderId"]

// ===== MULTI-TENANT HELPERS =====

// 2-level multi-tenant: /partitionKey (tenantId) + /userId
var users = CosmosDbContainerInfo.CreateMultiTenant("Users", "/userId");

// 3-level multi-tenant: /partitionKey (tenantId) + /folderId + /documentId
var documents = CosmosDbContainerInfo.CreateMultiTenant("Documents", "/folderId", "/documentId");

// ===== SETUP ALL CONTAINERS =====
app.CreateCosmosDatabaseAndContainers(
    products,  // 1 level
    users,     // 2 levels
    orders,    // 3 levels
    documents  // 3 levels (multi-tenant)
);
```

## 2. Entity Definitions

```csharp
using Clear.DataService.Entities.Cosmos;

// ===== FOR 2-LEVEL CONTAINER (/partitionKey + /userId) =====
public class TenantUser : BaseCosmosDbEntity
{
    // Level 1: Stored at /partitionKey
    public required string TenantId { get; set; }
  
    // Level 2: Stored at /userId
    public required string UserId { get; set; }
    
// Regular properties
    public required string Name { get; set; }
    public required string Email { get; set; }
}

// ===== FOR 3-LEVEL CONTAINER (/partitionKey + /customerId + /orderId) =====
public class TenantOrder : BaseCosmosDbEntity
{
    // Level 1: Stored at /partitionKey
    public required string TenantId { get; set; }
    
    // Level 2: Stored at /customerId
    public required string CustomerId { get; set; }
    
    // Level 3: Stored at /orderId
    public required string OrderId { get; set; }
    
    // Regular properties
    public decimal TotalAmount { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}
```

## 3. Creating Hierarchical Partition Keys

```csharp
using Clear.DataService.Models;

// ===== 2-LEVEL KEY =====
var key = HierarchicalPartitionKey.Create("tenant-123", "user-456");
// Matches: ["/partitionKey", "/userId"]

// ===== 3-LEVEL KEY =====
var key = HierarchicalPartitionKey.Create("tenant-123", "customer-789", "order-001");
// Matches: ["/partitionKey", "/customerId", "/orderId"]

// ===== USING HELPERS =====
var tenantKey = HierarchicalPartitionKey.ForTenant("tenant-123", "user-456");

// From delimited string
var keyFromString = HierarchicalPartitionKey.FromDelimited("tenant-123/user-456");
```

## 4. Save Operations

```csharp
// ===== SAVE TO 2-LEVEL CONTAINER =====
// Container: CreateHierarchical("Users", "/userId")
var user = new TenantUser
{
    Id = Guid.NewGuid().ToString(),
    TenantId = "tenant-123",     // Goes to /partitionKey
    UserId = "user-456",       // Goes to /userId
    Name = "John Doe",
    Email = "john@example.com"
};

var userKey = HierarchicalPartitionKey.Create("tenant-123", "user-456");
await cosmosContext.Save("Users", user, userKey);

// ===== SAVE TO 3-LEVEL CONTAINER =====
// Container: CreateHierarchical("Orders", "/customerId", "/orderId")
var order = new TenantOrder
{
    Id = Guid.NewGuid().ToString(),
    TenantId = "tenant-123",       // Goes to /partitionKey
    CustomerId = "customer-789",   // Goes to /customerId
    OrderId = "order-001",         // Goes to /orderId
    TotalAmount = 99.99m
};

var orderKey = HierarchicalPartitionKey.Create("tenant-123", "customer-789", "order-001");
await cosmosContext.Save("Orders", order, orderKey);
```

## 5. Query Operations

```csharp
// ===== QUERY WITH PARTIAL KEY (Level 1 only) =====
// Gets all users for a tenant
var tenantKey = HierarchicalPartitionKey.Create("tenant-123");
var allTenantUsers = await cosmosContext.GetList<TenantUser>("Users", tenantKey);

// ===== QUERY WITH FULL KEY (Levels 1 + 2) =====
// Gets specific user
var userKey = HierarchicalPartitionKey.Create("tenant-123", "user-456");
var specificUser = await cosmosContext.Get<TenantUser>("Users", userId, userKey);

// ===== QUERY WITH FILTER =====
var activeUsers = await cosmosContext.GetList<TenantUser>(
    "Users",
    FilterBuilder<TenantUser>.Create().Where(u => u.IsActive),
    tenantKey
);

// ===== PAGED QUERY =====
var pagedUsers = await cosmosContext.GetPagedList<TenantUser>(
    "Users",
    pageSize: 50,
    hierarchicalPartitionKey: tenantKey,
    sortBuilder: SortBuilder<TenantUser>.Create().OrderBy(u => u.Name)
);

// ===== QUERY 3-LEVEL CONTAINER =====
// Get all orders for a tenant + customer
var customerKey = HierarchicalPartitionKey.Create("tenant-123", "customer-789");
var customerOrders = await cosmosContext.GetList<TenantOrder>("Orders", customerKey);

// Get specific order (full 3-level key)
var specificOrderKey = HierarchicalPartitionKey.Create("tenant-123", "customer-789", "order-001");
var order = await cosmosContext.Get<TenantOrder>("Orders", orderId, specificOrderKey);
```

## 6. Batch Operations

**Important**: All items in a batch MUST have the same hierarchical partition key.

```csharp
// ===== BATCH FOR 2-LEVEL CONTAINER =====
var batchKey = HierarchicalPartitionKey.Create("tenant-123", "user-456");

var usersToCreate = new List<TenantUser>
{
    new() { Id = "1", TenantId = "tenant-123", UserId = "user-456", Name = "User 1", Email = "user1@test.com" },
    new() { Id = "2", TenantId = "tenant-123", UserId = "user-456", Name = "User 2", Email = "user2@test.com" },
    new() { Id = "3", TenantId = "tenant-123", UserId = "user-456", Name = "User 3", Email = "user3@test.com" }
};

foreach (var user in usersToCreate)
{
    cosmosContext.AddToBatch("Users", user, batchKey);
}

var results = await cosmosContext.SaveBatchAsync();

// ===== CHECK RESULTS =====
foreach (var result in results)
{
    if (result.Successful)
        Console.WriteLine($"? Batch succeeded for {result.ContainerName}");
    else
        Console.WriteLine($"? Batch failed: {result.Message}");
}
```

## 7. Repository Pattern

```csharp
using Clear.DataService.Abstractions;
using Clear.DataService.Repo;

// ===== REPOSITORY FOR 2-LEVEL CONTAINER =====
public interface ITenantUserRepository : ICosmosDbRepo<TenantUser>
{
    Task<List<TenantUser>> GetTenantUsersAsync(string tenantId);
    Task<TenantUser?> GetUserAsync(string tenantId, string userId, string id);
Task<TenantUser> CreateUserAsync(TenantUser user);
}

public class TenantUserRepository : BaseCosmosDbRepo<TenantUser>, ITenantUserRepository
{
    public TenantUserRepository(ICosmosDbContext context) : base(context, "Users") { }

    public async Task<List<TenantUser>> GetTenantUsersAsync(string tenantId)
    {
        // Query with Level 1 only
        var partitionKey = HierarchicalPartitionKey.Create(tenantId);
        return await Get(partitionKey);
 }

    public async Task<TenantUser?> GetUserAsync(string tenantId, string userId, string id)
    {
        // Query with full 2-level key
        var partitionKey = HierarchicalPartitionKey.Create(tenantId, userId);
return await Get(id, partitionKey);
    }

    public async Task<TenantUser> CreateUserAsync(TenantUser user)
    {
        var partitionKey = HierarchicalPartitionKey.Create(user.TenantId, user.UserId);
      return await Create(user, partitionKey);
    }
}
```

## 8. Multi-Tenant Application Setup

```csharp
// ===== DEFINE CONTAINER CONFIGURATION =====
public static class TenantContainers
{
    // 1 level: /partitionKey only
    public static readonly CosmosDbContainerInfo Settings = 
        CosmosDbContainerInfo.CreateSingle("Settings");
    
    // 2 levels: /partitionKey + /userId
    public static readonly CosmosDbContainerInfo Users = 
        CosmosDbContainerInfo.CreateMultiTenant("Users", "/userId");
    
    // 3 levels: /partitionKey + /customerId + /orderId
    public static readonly CosmosDbContainerInfo Orders = 
        CosmosDbContainerInfo.CreateMultiTenant("Orders", "/customerId", "/orderId");
    
    // 3 levels: /partitionKey + /folderId + /documentId
    public static readonly CosmosDbContainerInfo Documents = 
   CosmosDbContainerInfo.CreateMultiTenant("Documents", "/folderId", "/documentId");
}

// ===== SETUP IN PROGRAM.CS =====
app.CreateCosmosDatabaseAndContainers(
    TenantContainers.Settings,
    TenantContainers.Users,
    TenantContainers.Orders,
    TenantContainers.Documents
);
```

## 9. Service Pattern with Tenant Isolation

```csharp
public class TenantService
{
    private readonly ICosmosDbContext _context;
    private readonly string _tenantId;

    public TenantService(ICosmosDbContext context, string tenantId)
    {
        _context = context;
    _tenantId = tenantId;
    }

    public async Task<List<TenantUser>> GetAllUsersAsync()
    {
        // Query all users for this tenant (Level 1 only)
     var key = HierarchicalPartitionKey.Create(_tenantId);
  return await _context.GetList<TenantUser>("Users", key);
    }

    public async Task<TenantUser> CreateUserAsync(TenantUser user)
    {
        // Ensure tenant isolation
        user.TenantId = _tenantId;
        
        // Use full 2-level key
        var key = HierarchicalPartitionKey.Create(_tenantId, user.UserId);
        var doc = await _context.Save("Users", user, key);
        return doc.Data;
    }
}
```

## 10. Level-by-Level Querying

```csharp
// ===== QUERY AT DIFFERENT LEVELS FOR 3-LEVEL CONTAINER =====
// Container: CreateHierarchical("Orders", "/customerId", "/orderId")

// Level 1: All orders for a tenant
var level1Key = HierarchicalPartitionKey.Create("tenant-123");
var allTenantOrders = await cosmosContext.GetList<TenantOrder>("Orders", level1Key);

// Level 2: All orders for a tenant + customer
var level2Key = HierarchicalPartitionKey.Create("tenant-123", "customer-789");
var customerOrders = await cosmosContext.GetList<TenantOrder>("Orders", level2Key);

// Level 3: Specific order (full key)
var level3Key = HierarchicalPartitionKey.Create("tenant-123", "customer-789", "order-001");
var specificOrder = await cosmosContext.Get<TenantOrder>("Orders", orderId, level3Key);
```

## 11. Error Handling

```csharp
using Microsoft.Azure.Cosmos;

public async Task<TenantUser?> SafeGetUserAsync(string tenantId, string userId, string id)
{
    try
    {
     var key = HierarchicalPartitionKey.Create(tenantId, userId);
        return await _context.Get<TenantUser>("Users", id, key);
    }
    catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
        _logger.LogWarning("User not found: {TenantId}/{UserId}/{Id}", tenantId, userId, id);
        return null;
    }
    catch (ArgumentException ex)
    {
        _logger.LogError(ex, "Invalid partition key configuration");
        throw;
    }
}
```

## 12. Unit Testing

```csharp
using Xunit;

public class HierarchicalKeyTests
{
    private readonly ICosmosDbContext _context;

    public HierarchicalKeyTests(ICosmosDbContext context)
    {
        _context = context;
    }

    [Fact]
    public async Task Should_Save_And_Retrieve_With_2Level_Key()
    {
        // Arrange
        var tenantId = "test-tenant";
        var userId = "test-user";
        var user = new TenantUser
     {
        Id = Guid.NewGuid().ToString(),
    TenantId = tenantId,
       UserId = userId,
  Name = "Test User",
 Email = "test@example.com"
        };
        
     var partitionKey = HierarchicalPartitionKey.Create(tenantId, userId);

        // Act
        var savedDoc = await _context.Save("Users", user, partitionKey);
   var retrievedUser = await _context.Get<TenantUser>("Users", user.Id, partitionKey);

  // Assert
        Assert.NotNull(retrievedUser);
   Assert.Equal(user.Name, retrievedUser.Name);
        Assert.Equal(tenantId, retrievedUser.TenantId);
        Assert.Equal(userId, retrievedUser.UserId);
    }

    [Fact]
    public async Task Should_Save_And_Retrieve_With_3Level_Key()
    {
 // Arrange
        var tenantId = "test-tenant";
        var customerId = "test-customer";
 var orderId = "test-order";
        var order = new TenantOrder
        {
 Id = Guid.NewGuid().ToString(),
       TenantId = tenantId,
  CustomerId = customerId,
            OrderId = orderId,
   TotalAmount = 99.99m
        };
        
        var partitionKey = HierarchicalPartitionKey.Create(tenantId, customerId, orderId);

    // Act
      var savedDoc = await _context.Save("Orders", order, partitionKey);
   var retrievedOrder = await _context.Get<TenantOrder>("Orders", order.Id, partitionKey);

        // Assert
        Assert.NotNull(retrievedOrder);
  Assert.Equal(tenantId, retrievedOrder.TenantId);
    Assert.Equal(customerId, retrievedOrder.CustomerId);
        Assert.Equal(orderId, retrievedOrder.OrderId);
    }
}
```

## API Limits Summary

| Feature | Limit | Enforced By |
|---------|-------|-------------|
| Maximum partition key levels | 3 | Method signatures (compile-time) |
| Primary partition key path | `/partitionKey` | Fixed constant |
| Additional paths | 0-2 | Method overloads |
| Path format | Must start with `/` | Runtime validation |
| Reserved paths | Cannot use `/partitionKey` for levels 2-3 | Runtime validation |

## Summary

These examples demonstrate:
- ? Type-safe API with explicit 2nd and 3rd level parameters
- ? Enforcement of Azure Cosmos DB's 3-level maximum at compile time
- ? Consistent use of `/partitionKey` as the primary (Level 1) path
- ? How to define entities matching the partition key hierarchy
- ? How to create and use hierarchical partition keys
- ? How to perform CRUD operations at different hierarchy levels
- ? How to implement the repository pattern
- ? How to build multi-tenant applications
- ? How to handle errors properly
- ? How to write unit tests

For more information, see `HIERARCHICAL_PARTITION_KEYS.md`.
