# Clear.DataService

[![NuGet version](https://badge.fury.io/nu/Clear.DataService.svg)](https://badge.fury.io/nu/Clear.DataService)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/9.0)

A comprehensive data access library for .NET 9 that combines the power of Entity Framework Core with the flexibility of Dapper for SQL Server operations, plus complete Azure Cosmos DB support. Clear.DataService provides a unified approach to data operations across both relational and NoSQL databases.

## ? Features

### ??? Dual Database Support
- **SQL Server**: Entity Framework Core + Dapper integration for maximum flexibility
- **Azure Cosmos DB**: Complete document database operations with partition key support
- **Unified Patterns**: Consistent repository pattern across both database types

### ??? Architecture Excellence
- **Clean Architecture**: Well-defined abstractions and separation of concerns
- **Dependency Injection**: Full .NET Core DI container integration
- **Repository Pattern**: Built-in repository implementations for both database types
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
dotnet add package Clear.DataService --version 4.1.0
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

### SQL Repository Example

```csharp
public class User : BaseSqlDbEntity<int>
{
    public required string Name { get; set; }
    public required string Email { get; set; }
    public bool IsActive { get; set; } = true;
}

public interface IUserRepository : ISqlDbRepo<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<List<User>> GetActiveUsersAsync();
}

public class UserRepository : BaseSqlDbRepo<User>, IUserRepository
{
    public UserRepository(ISqlDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string email)
        => await Get(u => u.Email == email);

    public async Task<List<User>> GetActiveUsersAsync()
        => await Find(u => u.IsActive);
}
```

### Cosmos DB Repository Example

```csharp
public class Order : BaseCosmosDbEntity
{
    public required string CustomerId { get; set; }
    public required List<OrderItem> Items { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
}

public class OrderRepository : BaseCosmosDbRepo<Order>
{
    public OrderRepository(ICosmosDbContext context) : base(context, "Orders") { }

    public async Task<List<Order>> GetByCustomerAsync(string customerId)
        => await Get(customerId); // customerId is the partition key

    public async Task<Order> CreateOrderAsync(Order order, string customerId)
        => await Create(order, customerId);

    // New: Batch operations for high-performance bulk operations
    public async Task<List<CosmosBatchResult>> CreateOrdersBatchAsync(List<Order> orders, string customerId)
    {
        foreach (var order in orders)
        {
            Context.AddToBatch("Orders", order, customerId);
        }
        return await Context.SaveBatchAsync();
    }
}
```

### Service Integration

```csharp
public class ECommerceService
{
    private readonly IUserRepository _userRepository;     // SQL
    private readonly IOrderRepository _orderRepository;   // Cosmos DB

    public ECommerceService(IUserRepository userRepository, IOrderRepository orderRepository)
    {
        _userRepository = userRepository;
        _orderRepository = orderRepository;
    }

    public async Task<Order> ProcessOrderAsync(string customerId, List<OrderItem> items)
    {
        // Validate user exists in SQL database
        var user = await _userRepository.Get(int.Parse(customerId));
        if (user == null) throw new ArgumentException("User not found");

        // Create order in Cosmos DB
        var order = new Order
        {
            Id = Guid.NewGuid().ToString(),
            CustomerId = customerId,
            Items = items,
            TotalAmount = items.Sum(i => i.TotalPrice),
            Status = OrderStatus.Pending
        };

        return await _orderRepository.CreateOrderAsync(order, customerId);
    }

    // New: Process multiple orders efficiently using batch operations
    public async Task<List<CosmosBatchResult>> ProcessMultipleOrdersAsync(
        List<(string customerId, List<OrderItem> items)> orderRequests)
    {
        foreach (var (customerId, items) in orderRequests)
        {
            var order = new Order
            {
                Id = Guid.NewGuid().ToString(),
                CustomerId = customerId,
                Items = items,
                TotalAmount = items.Sum(i => i.TotalPrice),
                Status = OrderStatus.Pending
            };
            
            _orderRepository.Context.AddToBatch("Orders", order, customerId);
        }
        
        return await _orderRepository.Context.SaveBatchAsync();
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
