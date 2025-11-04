using Clear.DataService.Entities.Sql;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClearDataService.IntegrationTests.TestEntities;

/// <summary>
/// Test entity for SQL Server integration tests
/// </summary>
[Table("Products")]
public class SqlProduct : BaseSqlDbEntity<int>
{
 [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    public int StockQuantity { get; set; }
}

/// <summary>
/// Test entity with string Id for SQL Server
/// </summary>
[Table("Orders")]
public class SqlOrder : BaseSqlDbEntity<string>
{
    [MaxLength(100)]
    public string CustomerId { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Region { get; set; } = string.Empty;

 public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "Pending";

    public List<SqlOrderItem> Items { get; set; } = new();
}

[Table("OrderItems")]
public class SqlOrderItem : BaseSqlDbEntity<int>
{
    public string OrderId { get; set; } = string.Empty;

    [MaxLength(100)]
    public string ProductId { get; set; } = string.Empty;

    [MaxLength(200)]
  public string ProductName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [ForeignKey(nameof(OrderId))]
    public SqlOrder? Order { get; set; }
}
