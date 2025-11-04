using Clear.DataService.Abstractions;

namespace ClearDataService.IntegrationTests.TestEntities;

/// <summary>
/// Test entity for CosmosDB integration tests
/// </summary>
public class Product : ICosmosDbEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public int StockQuantity { get; set; }
}

/// <summary>
/// Test entity for hierarchical partition key scenarios
/// </summary>
public class Order : ICosmosDbEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CustomerId { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Pending";
    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
  public decimal UnitPrice { get; set; }
}
