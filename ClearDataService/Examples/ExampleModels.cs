using Clear.DataService.Abstractions;
using Clear.DataService.Entities.Cosmos;

namespace Clear.DataService.Examples;

/// <summary>
/// Example entity for demonstration purposes in the examples
/// </summary>
public class Product : ICosmosDbEntity
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public bool IsActive { get; set; }
}