using Microsoft.Azure.Cosmos;

namespace Clear.DataService.Models;

/// <summary>
/// Represents a hierarchical partition key for multi-level partitioning
/// Example: TenantId/ProductGroup/Category
/// </summary>
public class HierarchicalPartitionKey
{
    private readonly List<object> _keyValues;

    public HierarchicalPartitionKey(params object[] keyValues)
    {
        _keyValues = keyValues?.ToList() ?? throw new ArgumentNullException(nameof(keyValues));
        
        if (_keyValues.Count == 0)
        {
            throw new ArgumentException("At least one partition key value must be provided", nameof(keyValues));
        }
    }

    /// <summary>
    /// Creates a hierarchical partition key from individual components
    /// </summary>
    public static HierarchicalPartitionKey Create(params object[] keyValues)
    {
        return new HierarchicalPartitionKey(keyValues);
    }

    /// <summary>
    /// Creates a hierarchical partition key for tenant-based scenarios
    /// </summary>
    public static HierarchicalPartitionKey ForTenant(string tenantId, string? subKey1 = null, string? subKey2 = null)
    {
        var keys = new List<object> { tenantId };
        
        if (!string.IsNullOrEmpty(subKey1))
            keys.Add(subKey1);
            
        if (!string.IsNullOrEmpty(subKey2))
            keys.Add(subKey2);
            
        return new HierarchicalPartitionKey(keys.ToArray());
    }

    /// <summary>
    /// Converts to Cosmos DB PartitionKey
    /// </summary>
    public PartitionKey ToCosmosPartitionKey()
    {
        if (_keyValues.Count == 1)
        {
            // Single partition key
            var value = _keyValues[0];
            return value switch
            {
                string str => new PartitionKey(str),
                int i => new PartitionKey(i),
                double d => new PartitionKey(d),
                bool b => new PartitionKey(b),
                _ => new PartitionKey(value.ToString())
            };
        }
        
        // Hierarchical partition key (multiple values)
        var partitionKeyBuilder = new PartitionKeyBuilder();
        
        foreach (var value in _keyValues)
        {
            switch (value)
            {
                case string str:
                    partitionKeyBuilder.Add(str);
                    break;
                case int i:
                    partitionKeyBuilder.Add(i);
                    break;
                case double d:
                    partitionKeyBuilder.Add(d);
                    break;
                case bool b:
                    partitionKeyBuilder.Add(b);
                    break;
                default:
                    partitionKeyBuilder.Add(value.ToString());
                    break;
            }
        }
        
        return partitionKeyBuilder.Build();
    }

    /// <summary>
    /// Gets the partition key values as an array
    /// </summary>
    public object[] GetValues() => _keyValues.ToArray();

    /// <summary>
    /// Gets the number of partition key levels
    /// </summary>
    public int Levels => _keyValues.Count;

    /// <summary>
    /// Creates a string representation suitable for logging/debugging
    /// </summary>
    public override string ToString()
    {
        return string.Join("/", _keyValues);
    }

    /// <summary>
    /// Implicit conversion from string (for backward compatibility)
    /// </summary>
    public static implicit operator HierarchicalPartitionKey(string partitionKey)
    {
        return new HierarchicalPartitionKey(partitionKey);
    }

    /// <summary>
    /// Creates a hierarchical key from a delimited string
    /// </summary>
    public static HierarchicalPartitionKey FromDelimited(string delimitedKey, char delimiter = '/')
    {
        if (string.IsNullOrWhiteSpace(delimitedKey))
            throw new ArgumentException("Delimited key cannot be null or empty", nameof(delimitedKey));
            
        var parts = delimitedKey.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);
        return new HierarchicalPartitionKey(parts.Cast<object>().ToArray());
    }
}