# ClearDataService - Comprehensive Documentation

## Table of Contents
1. [Overview](#overview)
2. [Installation](#installation)
3. [Quick Start](#quick-start)
4. [Architecture](#architecture)
5. [SQL Database Features](#sql-database-features)
6. [Cosmos DB Features](#cosmos-db-features)
7. [Configuration](#configuration)
8. [Entities and Models](#entities-and-models)
9. [Repositories](#repositories)
10. [Contexts](#contexts)
11. [Migrations](#migrations)
12. [Exception Handling](#exception-handling)
13. [Advanced Features](#advanced-features)
14. [Best Practices](#best-practices)
15. [Examples](#examples)
16. [Troubleshooting](#troubleshooting)

## Overview

ClearDataService is a comprehensive data access library for .NET 9 applications that combines the power of Entity Framework Core with the flexibility of Dapper for SQL Server operations, and provides full support for Azure Cosmos DB. It offers a unified approach to data operations across both relational and NoSQL databases.

### Key Features
- **Dual Database Support**: SQL Server (via Entity Framework + Dapper) and Azure Cosmos DB
- **Repository Pattern**: Built-in repository implementations for both database types
- **Dependency Injection**: Full .NET Core DI container integration
- **Entity Framework Integration**: Leverage EF Core's ORM capabilities
- **Raw SQL Support**: Execute custom SQL queries using Dapper
- **Cosmos DB Document Management**: Complete document operations for NoSQL scenarios
- **Migration Support**: Automated database and container creation
- **Audit Entities**: Built-in audit trail support for entities
- **Flexible Querying**: LINQ support for both SQL and Cosmos DB
- **Batch Operations**: Efficient bulk operations for both SQL and Cosmos DB
- **Cosmos DB Transactional Batches**: High-performance batch processing with automatic partition key grouping
- **Configuration Management**: Multiple configuration options

## Installation

### Package Manager Console
```powershell
Install-Package ClearDataService -Version 3.0.1
```

### .NET CLI
```bash
dotnet add package ClearDataService --version 3.0.1
```

### PackageReference
```xml
<PackageReference Include="ClearDataService" Version="3.0.1" />
```

## Quick Start

### SQL Database Setup
```csharp
// Program.cs or Startup.cs
using ClearDataService;

// Register Entity Framework DbContext
builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseSqlServer(connectionString));

// Register ClearDataService SQL context
builder.Services.AddSqlDbContext();

// Optional: Auto-migrate database
app.MigrateSqlDatabase();
```

### Cosmos DB Setup
```csharp
// Using settings object
var cosmosSettings = CosmosDbSettings.Create(
    endpointUri: "https://your-account.documents.azure.com:443/",
    primaryKey: "your-primary-key",
    databaseName: "your-database-name"
);

builder.Services.AddCosmosDbContext(cosmosSettings);

// Or using configuration
builder.Services.AddCosmosDbContext(configuration, "CosmosDb");

// Create database and containers
app.CreateCosmosDatabaseAndContainers(
    new CosmosDbContainerInfo("Users", "/partitionKey"),
    new CosmosDbContainerInfo("Orders", "/customerId")
);
```

## Architecture

ClearDataService follows a layered architecture pattern:

```
???????????????????????????????????????
?           Application Layer         ?
???????????????????????????????????????
?         Repository Layer            ?
?  ????????????????????????????????????
?  ? SQL Repositories?Cosmos Repos   ??
?  ????????????????????????????????????
???????????????????????????????????????
?           Context Layer             ?
?  ????????????????????????????????????
?  ?   SqlDbContext  ?CosmosDbContext??
?  ????????????????????????????????????
???????????????????????????????????????
?          Data Access Layer          ?
?  ????????????????????????????????????
?  ? EF Core/Dapper  ? Cosmos Client ??
?  ????????????????????????????????????
???????????????????????????????????????
?           Database Layer            ?
?  ????????????????????????????????????
?  ?   SQL Server    ?   Cosmos DB   ??
?  ????????????????????????????????????
???????????????????????????????????????
```

## SQL Database Features

### ISqlDbContext Interface

The `ISqlDbContext` provides a comprehensive set of operations for SQL databases:

#### Read Operations
```csharp
public interface ISqlDbContext
{
    // Basic Get operations
    Task<T?> Get<T>(int id) where T : class;
    Task<T?> Get<T>(string id) where T : class;
    Task<List<T>> Get<T>(bool trackEntities = false) where T : class;
    Task<T?> Get<T>(Expression<Func<T, bool>> predicate, bool trackEntities = true) where T : class;
    Task<T?> GetOne<T>(bool trackEntities = false) where T : class;
    
    // Query operations
    Task<List<T>> Find<T>(Expression<Func<T, bool>> predicate, bool trackEntities = false) where T : class;
    IQueryable<T> GetAsQueryable<T>(bool trackEntities = false) where T : class;
    IQueryable<T> FindAsQueryable<T>(Expression<Func<T, bool>> predicate, bool trackEntities = false) where T : class;
    
    // Count and existence checks
    int Count<T>() where T : class;
    int Count<T>(Expression<Func<T, bool>> predicate) where T : class;
    Task<bool> Exists<T>(Expression<Func<T, bool>> predicate) where T : class;
}
```

#### Write Operations
```csharp
// Save operations
Task<T> Save<T>(T entity) where T : class;
Task<int> Save<T>(IEnumerable<T> entities) where T : class;

// Update operations
Task<T> Update<T>(T entity) where T : class;
Task<int> Update<T>(IEnumerable<T> entities) where T : class;

// Delete operations
Task<int> Delete<T>(T entity) where T : class;
Task<int> Delete<T>(Expression<Func<T, bool>> predicate) where T : class;
Task<int> Delete<T>(IEnumerable<T> entities) where T : class;
```

#### Batch Operations
```csharp
// Prepare entities for batch operations
void AddForInsert<T>(T entity) where T : class;
void AddAllForInsert<T>(IEnumerable<T> entities) where T : class;
void AddForDelete<T>(T entity) where T : class;
void AddAllForDelete<T>(IEnumerable<T> entities) where T : class;
void AddForUpdate<T>(T entity) where T : class;
void AddAllForUpdate<T>(IEnumerable<T> entities) where T : class;

// Execute batch operations
Task<int> SaveChanges();
```

#### Raw SQL Operations (via Dapper)
```csharp
// Execute SQL commands
Task<int> ExecuteSql(string sql);
Task<int> ExecuteSql(string sql, params object[] parameters);

// Query operations
Task<List<T>> Query<T>(string sql);
Task<List<T>> Query<T>(string sql, object parameters);
Task<T?> QueryFirstOrDefault<T>(string sql);
Task<T?> QueryFirstOrDefault<T>(string sql, object parameters);
```

### SQL Entity Framework Integration

Access Entity Framework features directly:
```csharp
DbSet<T> GetEntity<T>() where T : class;
```

## Cosmos DB Features

### ICosmosDbContext Interface

The `ICosmosDbContext` provides comprehensive Azure Cosmos DB operations:

#### Entity Operations
```csharp
public interface ICosmosDbContext
{
    // Get single entity
    Task<T> Get<T>(string containerName, string id, string? partitionKey = null, 
        CancellationToken cancellationToken = default) where T : ICosmosDbEntity;
    
    // Get entity with predicate
    Task<T?> Get<T>(string containerName, Func<T, bool> predicate, string? partitionKey = null, 
        CancellationToken cancellationToken = default) where T : ICosmosDbEntity;
    
    // Get multiple entities
    Task<List<T>> GetList<T>(string containerName, string? partitionKey = null, 
        CancellationToken cancellationToken = default) where T : ICosmosDbEntity;
    
    Task<List<T>> GetList<T>(string containerName, Func<T, bool> predicate, string? partitionKey = null, 
        CancellationToken cancellationToken = default) where T : ICosmosDbEntity;
}
```

#### Document Operations
```csharp
// Get documents with metadata
Task<CosmosDbDocument<T>> GetDocument<T>(string containerName, string id, string? partitionKey = null, 
    CancellationToken cancellationToken = default) where T : ICosmosDbEntity;

Task<CosmosDbDocument<T>?> GetDocument<T>(string containerName, Func<CosmosDbDocument<T>, bool> predicate, 
    string? partitionKey = null, CancellationToken cancellationToken = default) where T : ICosmosDbEntity;

Task<List<CosmosDbDocument<T>>> GetDocuments<T>(string containerName, string? partitionKey = null, 
    CancellationToken cancellationToken = default) where T : ICosmosDbEntity;

Task<List<CosmosDbDocument<T>>> GetDocuments<T>(string containerName, Func<CosmosDbDocument<T>, bool> predicate, 
    string? partitionKey = null, CancellationToken cancellationToken = default) where T : ICosmosDbEntity;
```

#### Write Operations
```csharp
// Create or update operations
Task<CosmosDbDocument<T>> Save<T>(string containerName, T entity, string partitionKey) where T : ICosmosDbEntity;
Task<CosmosDbDocument<T>> Upsert<T>(string containerName, T entity, string partitionKey) where T : ICosmosDbEntity;

// Delete operations
Task Delete<T>(string containerName, string id, string? partitionKey = null);

// Batch operations (New in v3.0.1)
void AddToBatch<T>(string containerName, T item, string partitionKey) where T : ICosmosDbEntity;
Task<List<CosmosBatchResult>> SaveBatchAsync();
```

### Cosmos DB Document Structure

Documents in Cosmos DB are wrapped in a `CosmosDbDocument<T>` structure:

```csharp
public class CosmosDbDocument<T> where T : ICosmosDbEntity
{
    public string Id { get; set; }           // Document ID
    public string EntityType { get; set; }   // Type information
    public string PartitionKey { get; set; } // Partition key
    public T Data { get; set; }              // Your entity data
    public string ETag { get; set; }         // Concurrency control
    public string ResourceId { get; set; }   // Cosmos resource ID
    public string SelfLink { get; set; }     // Self link
    public string Attachments { get; set; }  // Attachments
    public long TimestampSeconds { get; set; } // Unix timestamp
    public DateTime Timestamp { get; set; }  // DateTime timestamp
}
```

## Configuration

### SQL Database Configuration

#### Basic Setup
```csharp
builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddSqlDbContext();
```

#### With Custom DbContext
```csharp
public class MyDbContext : DbContext
{
    public MyDbContext(DbContextOptions<MyDbContext> options) : base(options) { }
    
    public DbSet<User> Users { get; set; }
    public DbSet<Order> Orders { get; set; }
}
```

### Cosmos DB Configuration

#### Using Settings Object
```csharp
var settings = CosmosDbSettings.Create(
    endpointUri: "https://your-account.documents.azure.com:443/",
    primaryKey: "your-primary-key",
    databaseName: "your-database-name"
);

builder.Services.AddCosmosDbContext(settings);
```

#### Using Configuration File
```json
// appsettings.json
{
  "CosmosDb": {
    "EndpointUri": "https://your-account.documents.azure.com:443/",
    "PrimaryKey": "your-primary-key",
    "DatabaseName": "your-database-name"
  }
}
```

```csharp
builder.Services.AddCosmosDbContext(configuration, "CosmosDb");
```

#### With Custom JSON Serialization
```csharp
var jsonSettings = new JsonSerializerSettings
{
    NullValueHandling = NullValueHandling.Ignore,
    DateFormatHandling = DateFormatHandling.IsoDateFormat
};

builder.Services.AddCosmosDbContext(settings, jsonSettings);
```

## Entities and Models

### SQL Database Entities

#### Basic Entity
```csharp
public class User : BaseSqlDbEntity<int>
{
    public required string Name { get; set; }
    public required string Email { get; set; }
    public DateTime CreatedDate { get; set; }
}
```

#### Entity with Audit Trail
```csharp
public class AuditedUser : BaseSqlDbAuditEntity<int, string>
{
    public required string Name { get; set; }
    public required string Email { get; set; }
}

// Usage
var user = new AuditedUser
{
    Id = 1,
    Name = "John Doe",
    Email = "john@example.com",
    Created = new AuditDetails<string>
    {
        Date = DateTime.UtcNow,
        UserId = "admin"
    }
};
```

#### Entity with Soft Delete
```csharp
public class SoftDeleteUser : BaseSqlDbAuditDeleteEntity<int, string>
{
    public required string Name { get; set; }
    public required string Email { get; set; }
}
```

### Cosmos DB Entities

#### Basic Cosmos Entity
```csharp
public class CosmosUser : BaseCosmosDbEntity
{
    public required string Name { get; set; }
    public required string Email { get; set; }
    public DateTime CreatedDate { get; set; }
}
```

#### Cosmos Entity with Audit Trail
```csharp
public class AuditedCosmosUser : BaseCosmosDbAuditEntity
{
    public required string Name { get; set; }
    public required string Email { get; set; }
}

// Usage
var user = new AuditedCosmosUser
{
    Id = Guid.NewGuid().ToString(),
    Name = "John Doe",
    Email = "john@example.com",
    Created = new AuditDetails
    {
        Date = DateTime.UtcNow,
        UserId = "admin"
    }
};
```

#### Cosmos Entity with Soft Delete
```csharp
public class SoftDeleteCosmosUser : BaseCosmosDbAuditDeleteEntity
{
    public required string Name { get; set; }
    public required string Email { get; set; }
}
```

## Repositories

### SQL Database Repositories

#### Basic Repository
```csharp
public interface IUserRepository : ISqlDbRepo<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<List<User>> GetActiveUsersAsync();
}

public class UserRepository : BaseSqlDbRepo<User>, IUserRepository
{
    public UserRepository(ISqlDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await Get(u => u.Email == email);
    }

    public async Task<List<User>> GetActiveUsersAsync()
    {
        return await Find(u => u.IsActive);
    }
}
```

#### Repository Registration
```csharp
// Manual registration
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Auto-registration (if using the extension methods)
builder.Services.AddAllSqlDbRepo(Assembly.GetExecutingAssembly());
```

### Cosmos DB Repositories

#### Basic Cosmos Repository
```csharp
public interface ICosmosUserRepository : ICosmosDbRepo<CosmosUser>
{
    Task<List<CosmosUser>> GetUsersByDomainAsync(string domain, string partitionKey);
}

public class CosmosUserRepository : BaseCosmosDbRepo<CosmosUser>, ICosmosUserRepository
{
    public CosmosUserRepository(ICosmosDbContext context) : base(context, "Users") { }

    public async Task<List<CosmosUser>> GetUsersByDomainAsync(string domain, string partitionKey)
    {
        return await Get(u => u.Email.EndsWith($"@{domain}"), partitionKey);
    }
}
```

## Contexts

### SQL Database Context Usage

```csharp
public class UserService
{
    private readonly ISqlDbContext _context;

    public UserService(ISqlDbContext context)
    {
        _context = context;
    }

    // Basic operations
    public async Task<User?> GetUserAsync(int id)
    {
        return await _context.Get<User>(id);
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        return await _context.Get<User>();
    }

    public async Task<User> CreateUserAsync(User user)
    {
        return await _context.Save(user);
    }

    // Batch operations
    public async Task CreateMultipleUsersAsync(List<User> users)
    {
        _context.AddAllForInsert(users);
        await _context.SaveChanges();
    }

    // Raw SQL operations
    public async Task<List<User>> GetUsersByCustomQueryAsync(string domain)
    {
        var sql = "SELECT * FROM Users WHERE Email LIKE @Domain";
        return await _context.Query<User>(sql, new { Domain = $"%{domain}" });
    }

    // Complex queries
    public async Task<List<User>> GetActiveUsersAsync()
    {
        return await _context.Find<User>(u => u.IsActive && u.CreatedDate > DateTime.UtcNow.AddYears(-1));
    }
}
```

### Cosmos DB Context Usage

```csharp
public class CosmosUserService
{
    private readonly ICosmosDbContext _context;
    private const string ContainerName = "Users";

    public CosmosUserService(ICosmosDbContext context)
    {
        _context = context;
    }

    // Basic operations
    public async Task<CosmosUser?> GetUserAsync(string id, string partitionKey)
    {
        return await _context.Get<CosmosUser>(ContainerName, id, partitionKey);
    }

    public async Task<List<CosmosUser>> GetUsersInPartitionAsync(string partitionKey)
    {
        return await _context.GetList<CosmosUser>(ContainerName, partitionKey);
    }

    public async Task<CosmosUser> CreateUserAsync(CosmosUser user, string partitionKey)
    {
        var document = await _context.Save(ContainerName, user, partitionKey);
        return document.Data;
    }

    // Document operations with metadata
    public async Task<CosmosDbDocument<CosmosUser>> GetUserDocumentAsync(string id, string partitionKey)
    {
        return await _context.GetDocument<CosmosUser>(ContainerName, id, partitionKey);
    }

    // Filtered operations
    public async Task<List<CosmosUser>> GetActiveUsersAsync(string partitionKey)
    {
        return await _context.GetList<CosmosUser>(ContainerName, u => u.IsActive, partitionKey);
    }

    // Batch operations (New in v3.0.1)
    public async Task<List<CosmosBatchResult>> CreateUsersBatchAsync(List<CosmosUser> users, string partitionKey)
    {
        // Queue users for batch processing
        foreach (var user in users)
        {
            _context.AddToBatch(ContainerName, user, partitionKey);
        }
        
        // Execute all batched operations
        return await _context.SaveBatchAsync();
    }

    public async Task<List<CosmosBatchResult>> ProcessMixedBatchAsync(
        List<CosmosUser> usersToCreate, 
        string partitionKey)
    {
        // Add multiple items to batch
        foreach (var user in usersToCreate)
        {
            _context.AddToBatch(ContainerName, user, partitionKey);
        }
        
        // Execute batch and get results
        var results = await _context.SaveBatchAsync();
        
        // Process results
        var successCount = results.Count(r => r.Successful);
        var failureCount = results.Count(r => !r.Successful);
        
        _logger.LogInformation("Batch completed: {SuccessCount} succeeded, {FailureCount} failed", 
            successCount, failureCount);
            
        return results;
    }
}
```

## Migrations

### SQL Database Migrations

```csharp
// Automatic migration on startup
public static void Main(string[] args)
{
    var app = CreateHostBuilder(args).Build();
    
    // Apply migrations
    app.MigrateSqlDatabase();
    
    app.Run();
}
```

### Cosmos DB Migrations

#### Container Creation
```csharp
// Define container information
var containers = new[]
{
    new CosmosDbContainerInfo("Users", "/partitionKey"),
    new CosmosDbContainerInfo("Orders", "/customerId"),
    new CosmosDbContainerInfo("Products", "/categoryId")
};

// Create database and containers
app.CreateCosmosDatabaseAndContainers(containers);
```

#### Using Container Models
```csharp
public static class CosmosContainers
{
    public static readonly CosmosDbContainerInfo Users = new("Users", "/partitionKey");
    public static readonly CosmosDbContainerInfo Orders = new("Orders", "/customerId");
    public static readonly CosmosDbContainerInfo Products = new("Products", "/categoryId");
}

// Usage
app.CreateCosmosDatabaseAndContainers(
    CosmosContainers.Users,
    CosmosContainers.Orders,
    CosmosContainers.Products
);
```

## Exception Handling

### Built-in Exceptions

```csharp
// Cosmos DB specific exceptions
public class ContainerNameEmptyException : BaseException
public class ContainerNameMissingFromRepoException : BaseException
public class PartitionKeyNullException : BaseException
public class CosmosJsonSerializerNullException : BaseException
```

### Exception Handling Examples

```csharp
public class UserService
{
    public async Task<User?> GetUserSafelyAsync(string id)
    {
        try
        {
            return await _context.Get<User>(id);
        }
        catch (ContainerNameEmptyException ex)
        {
            _logger.LogError(ex, "Container name is empty");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}", id);
            throw;
        }
    }
}
```

## Advanced Features

### Custom JSON Serialization for Cosmos DB

```csharp
var jsonSettings = new JsonSerializerSettings
{
    NullValueHandling = NullValueHandling.Ignore,
    DateFormatHandling = DateFormatHandling.IsoDateFormat,
    ContractResolver = new CamelCasePropertyNamesContractResolver()
};

builder.Services.AddCosmosDbContext(cosmosSettings, jsonSettings);
```

### Entity Tracking Control

```csharp
// Disable tracking for read-only operations
var users = await _context.Get<User>(trackEntities: false);
var queryableUsers = _context.GetAsQueryable<User>(trackEntities: false);
```

### Batch Operations

#### SQL Database Batch Operations
```csharp
public async Task ProcessUserBatchAsync(List<User> usersToInsert, List<User> usersToUpdate, List<User> usersToDelete)
{
    // Prepare batch operations
    _context.AddAllForInsert(usersToInsert);
    _context.AddAllForUpdate(usersToUpdate);
    _context.AddAllForDelete(usersToDelete);
    
    // Execute all operations in a single transaction
    var affectedRows = await _context.SaveChanges();
    
    _logger.LogInformation("Processed {Count} entities", affectedRows);
}
```

#### Cosmos DB Batch Operations (New in v3.0.1)
```csharp
public async Task ProcessCosmosBatchAsync(List<CosmosUser> users, string partitionKey)
{
    try
    {
        // Queue entities for batch processing
        foreach (var user in users)
        {
            _context.AddToBatch("Users", user, partitionKey);
        }
        
        // Execute batch operations
        var results = await _context.SaveBatchAsync();
        
        // Process results
        foreach (var result in results)
        {
            if (result.Successful)
            {
                _logger.LogInformation("Batch operation succeeded: {Result}", result);
            }
            else
            {
                _logger.LogError("Batch operation failed: {Result}", result);
            }
        }
        
        // Summary
        var successCount = results.Count(r => r.Successful);
        var totalCount = results.Count;
        
        _logger.LogInformation("Batch completed: {SuccessCount}/{TotalCount} operations succeeded", 
            successCount, totalCount);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error processing Cosmos DB batch operation");
        throw;
    }
}

// Mixed container batch processing
public async Task ProcessMultiContainerBatchAsync()
{
    // Add items to different containers
    _context.AddToBatch("Users", user1, "partition1");
    _context.AddToBatch("Orders", order1, "customer1");
    _context.AddToBatch("Products", product1, "category1");
    
    // All operations are executed together, grouped by container and partition
    var results = await _context.SaveBatchAsync();
    
    // Results are returned for all containers
    var userResults = results.Where(r => r.ContainerName == "Users");
    var orderResults = results.Where(r => r.ContainerName == "Orders");
    var productResults = results.Where(r => r.ContainerName == "Products");
}
```

#### Batch Operation Best Practices
```csharp
public class CosmosDbBatchService
{
    private readonly ICosmosDbContext _context;
    private const int MaxBatchSize = 100; // Cosmos DB transactional batch limit

    public async Task<List<CosmosBatchResult>> ProcessLargeBatchAsync<T>(
        string containerName, 
        List<T> entities, 
        string partitionKey) where T : ICosmosDbEntity
    {
        var allResults = new List<CosmosBatchResult>();
        
        // Process in chunks to respect Cosmos DB batch limits
        for (int i = 0; i < entities.Count; i += MaxBatchSize)
        {
            var chunk = entities.Skip(i).Take(MaxBatchSize).ToList();
            
            foreach (var entity in chunk)
            {
                _context.AddToBatch(containerName, entity, partitionKey);
            }
            
            var batchResults = await _context.SaveBatchAsync();
            allResults.AddRange(batchResults);
            
            // Optional: Add delay between batches to avoid throttling
            if (i + MaxBatchSize < entities.Count)
            {
                await Task.Delay(100); // 100ms delay
            }
        }
        
        return allResults;
    }
}
```

### Connection Configuration

```csharp
// Custom Cosmos client options
var cosmosClientOptions = new CosmosClientOptions
{
    ConnectionMode = ConnectionMode.Direct,
    PortReuseMode = PortReuseMode.PrivatePortPool,
    IdleTcpConnectionTimeout = TimeSpan.FromMinutes(60),
    MaxRetryAttemptsOnRateLimitedRequests = 5,
    MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(30)
};
```

## Best Practices

### SQL Database Best Practices

1. **Entity Tracking**: Disable tracking for read-only operations
2. **Batch Operations**: Use batch operations for multiple entity operations
3. **Query Optimization**: Use `GetAsQueryable` for complex LINQ queries
4. **Raw SQL**: Use raw SQL for complex queries that are difficult in LINQ
5. **Connection Management**: Let the framework handle connection lifecycle

```csharp
// Good: Disable tracking for read-only scenarios
var users = await _context.Get<User>(trackEntities: false);

// Good: Use batch operations
_context.AddAllForInsert(newUsers);
await _context.SaveChanges();

// Good: Efficient querying
var activeUsers = _context.GetAsQueryable<User>(trackEntities: false)
    .Where(u => u.IsActive)
    .OrderBy(u => u.Name)
    .Take(100);
```

### Cosmos DB Best Practices

1. **Partition Key Design**: Choose partition keys that distribute data evenly
2. **Query Efficiency**: Include partition key in queries when possible
3. **Batch Operations**: Use transactional batches for multiple operations within the same partition
4. **Batch Size Limits**: Respect Cosmos DB's 100-operation limit per transactional batch
5. **Error Handling**: Handle throttling and temporary failures, especially for batch operations
6. **Cost Optimization**: Use appropriate consistency levels and batch operations to reduce RU consumption

```csharp
// Good: Include partition key in queries
var user = await _context.Get<CosmosUser>("Users", userId, partitionKey);

// Good: Efficient batch processing
var users = await _context.GetList<CosmosUser>("Users", partitionKey);

// Good: Handle partition-specific operations
public async Task<List<CosmosUser>> GetUsersByCompanyAsync(string companyId)
{
    // companyId is the partition key
    return await _context.GetList<CosmosUser>("Users", companyId);
}

// Good: Use batch operations for multiple entities in same partition
public async Task<List<CosmosBatchResult>> CreateCompanyUsersAsync(string companyId, List<CosmosUser> users)
{
    foreach (var user in users)
    {
        _context.AddToBatch("Users", user, companyId);
    }
    return await _context.SaveBatchAsync();
}

// Good: Handle batch results properly
public async Task ProcessBatchWithErrorHandlingAsync(List<CosmosUser> users, string partitionKey)
{
    foreach (var user in users)
    {
        _context.AddToBatch("Users", user, partitionKey);
    }
    
    var results = await _context.SaveBatchAsync();
    var failures = results.Where(r => !r.Successful).ToList();
    
    if (failures.Any())
    {
        _logger.LogWarning("Batch operation had {FailureCount} failures", failures.Count);
        foreach (var failure in failures)
        {
            _logger.LogError("Batch failure: {Error}", failure.Message);
        }
    }
}
```

### General Best Practices

1. **Dependency Injection**: Always use constructor injection
2. **Async/Await**: Use async methods consistently
3. **Exception Handling**: Handle database-specific exceptions
4. **Logging**: Implement comprehensive logging
5. **Testing**: Write unit tests with mocked contexts

## Examples

### Complete SQL Entity Example

```csharp
// Entity definition
public class Product : BaseSqlDbAuditEntity<int, string>
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public decimal Price { get; set; }
    public int CategoryId { get; set; }
    public bool IsActive { get; set; } = true;
}

// Repository interface
public interface IProductRepository : ISqlDbRepo<Product>
{
    Task<List<Product>> GetByCategoryAsync(int categoryId);
    Task<List<Product>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice);
    Task<Product?> GetByNameAsync(string name);
}

// Repository implementation
public class ProductRepository : BaseSqlDbRepo<Product>, IProductRepository
{
    public ProductRepository(ISqlDbContext context) : base(context) { }

    public async Task<List<Product>> GetByCategoryAsync(int categoryId)
    {
        return await Find(p => p.CategoryId == categoryId && p.IsActive);
    }

    public async Task<List<Product>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice)
    {
        return await Find(p => p.Price >= minPrice && p.Price <= maxPrice && p.IsActive);
    }

    public async Task<Product?> GetByNameAsync(string name)
    {
        return await Get(p => p.Name == name);
    }
}

// Service usage
public class ProductService
{
    private readonly IProductRepository _repository;

    public ProductService(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<Product> CreateProductAsync(string name, string description, decimal price, int categoryId, string userId)
    {
        var product = new Product
        {
            Name = name,
            Description = description,
            Price = price,
            CategoryId = categoryId,
            Created = new AuditDetails<string>
            {
                Date = DateTime.UtcNow,
                UserId = userId
            }
        };

        await _repository.Create(product);
        return product;
    }
}
```

### Complete Cosmos DB Entity Example

```csharp
// Entity definition
public class Order : BaseCosmosDbAuditEntity
{
    public required string CustomerId { get; set; }
    public required List<OrderItem> Items { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime OrderDate { get; set; }
}

public class OrderItem
{
    public required string ProductId { get; set; }
    public required string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice => Quantity * UnitPrice;
}

public enum OrderStatus
{
    Pending,
    Processing,
    Shipped,
    Delivered,
    Cancelled
}

// Repository interface
public interface IOrderRepository : ICosmosDbRepo<Order>
{
    Task<List<Order>> GetByCustomerAsync(string customerId);
    Task<List<Order>> GetByStatusAsync(OrderStatus status, string customerId);
    Task<Order?> GetByOrderNumberAsync(string orderNumber, string customerId);
}

// Repository implementation
public class OrderRepository : BaseCosmosDbRepo<Order>, IOrderRepository
{
    public OrderRepository(ICosmosDbContext context) : base(context, "Orders") { }

    public async Task<List<Order>> GetByCustomerAsync(string customerId)
    {
        return await Get(customerId); // customerId is the partition key
    }

    public async Task<List<Order>> GetByStatusAsync(OrderStatus status, string customerId)
    {
        return await Get(o => o.Status == status, customerId);
    }

    public async Task<Order?> GetByOrderNumberAsync(string orderNumber, string customerId)
    {
        return await Get(o => o.Id == orderNumber, customerId);
    }
}

// Service usage
public class OrderService
{
    private readonly IOrderRepository _repository;

    public OrderService(IOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<Order> CreateOrderAsync(string customerId, List<OrderItem> items, string userId)
    {
        var order = new Order
        {
            Id = Guid.NewGuid().ToString(),
            CustomerId = customerId,
            Items = items,
            TotalAmount = items.Sum(i => i.TotalPrice),
            Status = OrderStatus.Pending,
            OrderDate = DateTime.UtcNow,
            Created = new AuditDetails
            {
                Date = DateTime.UtcNow,
                UserId = userId
            }
        };

        return await _repository.Create(order, customerId);
    }
}
```

### Mixed Database Application Example

```csharp
// Application using both SQL and Cosmos DB
public class ECommerceService
{
    private readonly IProductRepository _productRepository; // SQL
    private readonly IOrderRepository _orderRepository;     // Cosmos DB
    private readonly ILogger<ECommerceService> _logger;

    public ECommerceService(
        IProductRepository productRepository,
        IOrderRepository orderRepository,
        ILogger<ECommerceService> logger)
    {
        _productRepository = productRepository;
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task<Order> ProcessOrderAsync(string customerId, List<CreateOrderItemRequest> itemRequests, string userId)
    {
        try
        {
            // Validate products exist in SQL database
            var orderItems = new List<OrderItem>();
            foreach (var request in itemRequests)
            {
                var product = await _productRepository.Get(request.ProductId);
                if (product == null)
                {
                    throw new ArgumentException($"Product {request.ProductId} not found");
                }

                orderItems.Add(new OrderItem
                {
                    ProductId = product.Id.ToString(),
                    ProductName = product.Name,
                    Quantity = request.Quantity,
                    UnitPrice = product.Price
                });
            }

            // Create order in Cosmos DB
            var order = await _orderRepository.Create(new Order
            {
                Id = Guid.NewGuid().ToString(),
                CustomerId = customerId,
                Items = orderItems,
                TotalAmount = orderItems.Sum(i => i.TotalPrice),
                Status = OrderStatus.Pending,
                OrderDate = DateTime.UtcNow,
                Created = new AuditDetails
                {
                    Date = DateTime.UtcNow,
                    UserId = userId
                }
            }, customerId);

            _logger.LogInformation("Order {OrderId} created for customer {CustomerId}", order.Id, customerId);
            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing order for customer {CustomerId}", customerId);
            throw;
        }
    }
}
```

## Troubleshooting

### Common Issues and Solutions

#### SQL Database Issues

**Issue**: DbContext not registered
```
Error: Unable to resolve service for type 'ISqlDbContext'
```
**Solution**: Ensure both DbContext and ClearDataService are registered:
```csharp
builder.Services.AddDbContext<MyDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddSqlDbContext();
```

**Issue**: Entity not found
```
Error: The entity type 'User' requires a primary key to be defined
```
**Solution**: Ensure your entity inherits from a base entity or has a proper key:
```csharp
public class User : BaseSqlDbEntity<int>
{
    // Properties
}
```

#### Cosmos DB Issues

**Issue**: Container not found
```
Error: Response status code does not indicate success: NotFound
```
**Solution**: Ensure containers are created before use:
```csharp
app.CreateCosmosDatabaseAndContainers(new CosmosDbContainerInfo("Users", "/partitionKey"));
```

**Issue**: Partition key mismatch
```
Error: Partition key provided either doesn't correspond to definition in the collection
```
**Solution**: Ensure partition key values match the container's partition key path:
```csharp
// Container defined with "/customerId" partition key
await _context.Get<Order>("Orders", orderId, customerId); // Use customerId as partition key
```

**Issue**: Request rate too large (429)
```
Error: Request rate is large
```
**Solution**: Implement retry logic or increase RU/s:
```csharp
var cosmosClientOptions = new CosmosClientOptions
{
    MaxRetryAttemptsOnRateLimitedRequests = 10,
    MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(60)
};
```

### Performance Optimization

#### SQL Database
1. **Indexing**: Ensure proper indexes on frequently queried columns
2. **Query optimization**: Use `GetAsQueryable` for complex queries
3. **Batch operations**: Use batch operations for multiple entities
4. **Connection pooling**: Configure connection pool size appropriately

#### Cosmos DB
1. **Partition key design**: Choose partition keys that distribute load evenly
2. **Query optimization**: Always include partition key in queries when possible
3. **RU optimization**: Monitor and optimize Request Unit consumption
4. **Indexing policy**: Configure appropriate indexing policies

### Debugging Tips

1. **Enable logging**: Configure detailed logging for Entity Framework and Cosmos DB
```csharp
builder.Services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
```

2. **Monitor queries**: Use application insights or similar tools to monitor database calls

3. **Test with mock data**: Use in-memory databases for testing:
```csharp
services.AddDbContext<TestDbContext>(options => options.UseInMemoryDatabase("TestDb"));
```

4. **Validate configuration**: Ensure connection strings and settings are correct

---

This comprehensive documentation covers all aspects of ClearDataService. For additional support or feature requests, please refer to the project repository or contact the development team.