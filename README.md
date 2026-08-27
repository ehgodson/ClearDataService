# Clear.DataService

[![NuGet version](https://badge.fury.io/nu/Clear.DataService.svg)](https://badge.fury.io/nu/Clear.DataService)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)

A comprehensive data access library for .NET 10 that combines the power of Entity Framework Core with the flexibility of Dapper for SQL Server operations, plus complete Azure Cosmos DB support. Clear.DataService provides a unified approach to data operations across both relational and NoSQL databases.

## ? Features

### ??? Dual Database Support
- **SQL Server**: Entity Framework Core + Dapper integration for maximum flexibility
- **Azure Cosmos DB**: Complete document database operations with partition key support
- **Unified Contexts**: Explicit SQL and Cosmos DB context APIs

### ??? Architecture Excellence
- **Clean Architecture**: Well-defined abstractions and separation of concerns
- **Dependency Injection**: Full .NET Core DI container integration
- **Direct Data Access**: Inject the context required by each application service
- **Entity Framework Integration**: Leverage EF Core's ORM capabilities
- **Raw SQL Support**: Execute custom SQL queries using Dapper

### ?? Entity Management
- **Base Entity Classes**: Pre-built entities with audit trail support
- **Flexible Querying**: LINQ support for both SQL and Cosmos DB
- **Batch Operations**: Efficient bulk operations for both SQL and Cosmos DB
- **Cosmos DB Batch Processing**: Transactional batch operations with automatic partition key grouping and smart chunking (max 100 ops per batch)
- **Hierarchical Partition Keys**: Full support for up to 3-level partition keys in Cosmos DB
- **Soft Delete**: Built-in soft delete functionality
- **Audit Trails**: Automatic creation and modification tracking

### ?? Developer Experience
- **Migration Support**: Automated database and container creation
- **Configuration Management**: Multiple configuration options
- **Exception Handling**: Comprehensive error handling with custom exceptions
- **Extensive Documentation**: Complete guides and examples
- **Testing Support**: Built-in testing utilities and mocks

## ?? Quick Start

### Installation

```bash
dotnet add package Clear.DataService --version 6.0.0
```

### SQL Database Setup

```csharp
// Program.cs
using Clear.DataService;

// Register Entity Framework DbContext
builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseSqlServer(connectionString));

// Register Clear.DataService SQL context
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

// Create database and containers
app.CreateCosmosDatabaseAndContainers(
    new CosmosDbContainerInfo("Users", "/partitionKey"),
    new CosmosDbContainerInfo("Orders", "/customerId")
);
```

## ?? Usage Examples

### SQL Context Example

```csharp
public class User : BaseSqlDbEntity<int>
{
    public required string Name { get; set; }
    public required string Email { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UserService
{
    private readonly ISqlDbContext _context;

    public UserService(ISqlDbContext context) => _context = context;

    public async Task<User?> GetByEmailAsync(string email)
        => await _context.Get<User>(u => u.Email == email, trackEntities: false);

    public async Task<List<User>> GetActiveUsersAsync()
        => await _context.Find<User>(u => u.IsActive);
}
```

### Cosmos DB Context Example

```csharp
public class Order : BaseCosmosDbEntity
{
    public required string CustomerId { get; set; }
    public required List<OrderItem> Items { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
}

public class OrderService
{
    private readonly ICosmosDbContext _context;

    public OrderService(ICosmosDbContext context) => _context = context;

    public async Task<List<Order>> GetByCustomerAsync(string customerId)
        => await _context.GetList<Order>("Orders", customerId);

    public async Task<Order> CreateOrderAsync(Order order, string customerId)
        => (await _context.Save("Orders", order, customerId)).Data;

    // New: Batch operations for high-performance bulk operations
    public async Task<List<CosmosBatchResult>> CreateOrdersBatchAsync(List<Order> orders, string customerId)
    {
        foreach (var order in orders)
        {
            _context.AddToBatch("Orders", order, customerId);
        }
        return await _context.SaveBatchAsync();
    }
}
```

## ?? Configuration

### SQL Database Configuration

```csharp
// Basic setup
builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddSqlDbContext();

// With auto-migration
app.MigrateSqlDatabase();
```

### Cosmos DB Configuration

```csharp
// Using configuration file
// appsettings.json
{
  "CosmosDb": {
    "EndpointUri": "https://your-account.documents.azure.com:443/",
    "PrimaryKey": "your-primary-key",
    "DatabaseName": "your-database-name"
  }
}

// Register service
builder.Services.AddCosmosDbContext(configuration, "CosmosDb");
```

## ?? Documentation

For comprehensive documentation, examples, and advanced usage patterns, see:
- [Complete Documentation](./Clear.DataService-Documentation.md) - Detailed guide covering all features
- [Changelog](./Clear.DataService/CHANGELOG.md) - Version history and changes
- [Examples Repository](https://github.com/ehgodson/Clear.DataService/tree/main/examples) - Sample applications

## ??? Architecture

```
???????????????????????????????????????
?           Application Layer         ?
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
```

## ?? Contributing

We welcome contributions! Please see our [Contributing Guidelines](CONTRIBUTING.md) for details.

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Ensure all tests pass
6. Submit a pull request

## ?? License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## ?? Acknowledgments

- Built with ?? by [Godwin Ehichoya](https://github.com/ehgodson)
- Powered by [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- Enhanced with [Dapper](https://github.com/DapperLib/Dapper)
- Cosmos DB support via [Azure Cosmos DB .NET SDK](https://docs.microsoft.com/en-us/azure/cosmos-db/sql/sql-api-dotnet-v3sdk-preview)

## ?? Support

- ?? Email: [support@clearwox.com](mailto:support@clearwox.com)
- ?? Issues: [GitHub Issues](https://github.com/ehgodson/Clear.DataService/issues)
- ?? Discussions: [GitHub Discussions](https://github.com/ehgodson/Clear.DataService/discussions)

---

**Made with ?? by [Clearwox Systems](https://clearwox.com)**
