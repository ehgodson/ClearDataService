using Microsoft.Azure.Cosmos;

namespace Clear.DataService.Models;

/// <summary>
/// Represents a hierarchical partition key for multi-level partitioning.
/// Azure Cosmos DB supports up to 3 levels of hierarchical partition keys.
/// Supported types: string, int, double, bool
/// Example: TenantId/UserId/DocumentId
/// </summary>
public class CosmosDbPartitionKey
{
    private readonly List<PartitionKeyValue> _keyValues;

    /// <summary>
    /// Maximum number of partition key levels supported by Azure Cosmos DB
    /// </summary>
    public const int MaxLevels = 3;

    private CosmosDbPartitionKey(params PartitionKeyValue[] keyValues)
    {
        if (keyValues == null || keyValues.Length == 0)
        {
            throw new ArgumentException("At least one partition key value must be provided", nameof(keyValues));
        }

        if (keyValues.Length > MaxLevels)
        {
            throw new ArgumentException($"Azure Cosmos DB supports a maximum of {MaxLevels} hierarchical partition key levels. Provided: {keyValues.Length}", nameof(keyValues));
        }

        _keyValues = [.. keyValues];
    }

    // ============================================
    // FLUENT API - Simplified with implicit conversions
    // ============================================

    /// <summary>
    /// Creates a single-level partition key.
    /// Supports: string, int, double, bool
    /// Example: HierarchicalPartitionKey.Create("tenant-123")
    /// Example: HierarchicalPartitionKey.Create(12345)
    /// </summary>
    public static CosmosDbPartitionKey Create(PartitionKeyValue level1)
    {
        return new CosmosDbPartitionKey(level1);
    }

    /// <summary>
    /// Creates a 2-level hierarchical partition key.
    /// Supports: string, int, double, bool for each level
    /// Example: HierarchicalPartitionKey.Create("tenant-123", "user-456")
    /// Example: HierarchicalPartitionKey.Create("tenant", 456)
    /// </summary>
    public static CosmosDbPartitionKey Create(PartitionKeyValue level1, PartitionKeyValue level2)
    {
        return new CosmosDbPartitionKey(level1, level2);
    }

    /// <summary>
    /// Creates a 3-level hierarchical partition key (maximum).
    /// Supports: string, int, double, bool for each level
    /// Example: HierarchicalPartitionKey.Create("tenant-123", "customer-456", "order-789")
    /// Example: HierarchicalPartitionKey.Create("tenant", 456, true)
    /// </summary>
    public static CosmosDbPartitionKey Create(PartitionKeyValue level1, PartitionKeyValue level2, PartitionKeyValue level3)
    {
        return new CosmosDbPartitionKey(level1, level2, level3);
    }

    /// <summary>
    /// Starts building a hierarchical partition key with the first level.
    /// Supports: string, int, double, bool
    /// Example: HierarchicalPartitionKey.WithLevel1("tenant-123").AddLevel2("user-456")
    /// Example: HierarchicalPartitionKey.WithLevel1(12345).AddLevel2(true)
    /// </summary>
    public static CosmosDbPartitionKey WithLevel1(PartitionKeyValue level1)
    {
        return new CosmosDbPartitionKey(level1);
    }

    // ============================================
    // FLUENT API - Level Addition Methods
    // ============================================

    /// <summary>
    /// Adds a second level to create a 2-level hierarchical partition key.
    /// Supports: string, int, double, bool
    /// Example: WithLevel1("tenant").AddLevel2("user")
    /// Example: WithLevel1("tenant").AddLevel2(456)
    /// </summary>
    public CosmosDbPartitionKey AddLevel2(PartitionKeyValue level2)
    {
        if (_keyValues.Count >= 2)
        {
            throw new InvalidOperationException("Second level already configured.");
        }

        var newValues = new List<PartitionKeyValue>(_keyValues) { level2 };
        return new CosmosDbPartitionKey(newValues.ToArray());
    }

    /// <summary>
    /// Adds a third level to create a 3-level hierarchical partition key.
    /// Must be called after AddLevel2().
    /// Supports: string, int, double, bool
    /// Example: WithLevel1("tenant").AddLevel2("user").AddLevel3("document")
    /// Example: WithLevel1("tenant").AddLevel2(456).AddLevel3(true)
    /// </summary>
    public CosmosDbPartitionKey AddLevel3(PartitionKeyValue level3)
    {
        if (_keyValues.Count < 2)
        {
            throw new InvalidOperationException("Must add second level before adding third level.");
        }

        if (_keyValues.Count >= 3)
        {
            throw new InvalidOperationException("Third level already configured.");
        }

        var newValues = new List<PartitionKeyValue>(_keyValues) { level3 };
        return new CosmosDbPartitionKey(newValues.ToArray());
    }

    // ============================================
    // SPECIALIZED CREATION METHODS
    // ============================================

    /// <summary>
    /// Creates a hierarchical partition key for tenant-based scenarios.
    /// Supports 1-3 levels: tenant, tenant+subKey1, tenant+subKey1+subKey2
    /// Example: ForTenant("tenant-123", "user-456", "doc-789")
    /// </summary>
    public static CosmosDbPartitionKey ForTenant(string tenantId, string? subKey1 = null, string? subKey2 = null)
    {
        var keys = new List<PartitionKeyValue> { tenantId };

        if (!string.IsNullOrEmpty(subKey1))
            keys.Add(subKey1);

        if (!string.IsNullOrEmpty(subKey2))
            keys.Add(subKey2);

        return new CosmosDbPartitionKey(keys.ToArray());
    }

    // ============================================
    // CONVERSION & UTILITY METHODS
    // ============================================

    /// <summary>
    /// Converts to Cosmos DB PartitionKey
    /// </summary>
    public PartitionKey ToCosmosPartitionKey()
    {
        if (_keyValues.Count == 1)
        {
            // Single partition key
            return _keyValues[0].ToPartitionKey();
        }

        // Hierarchical partition key (multiple values)
        var partitionKeyBuilder = new PartitionKeyBuilder();

        foreach (var value in _keyValues)
        {
            value.AddToBuilder(partitionKeyBuilder);
        }

        return partitionKeyBuilder.Build();
    }

    /// <summary>
    /// Gets the partition key values as an array of strings (for display/logging)
    /// </summary>
    public string[] GetValues() => [.. _keyValues.Select(v => v.ToString())];

    /// <summary>
    /// Gets the number of partition key levels (1-3)
    /// </summary>
    public int Levels => _keyValues.Count;

    /// <summary>
    /// Creates a string representation suitable for logging/debugging
    /// </summary>
    public override string ToString()
    {
        return _keyValues[0].ToString();
    }

    /// <summary>
    /// Implicit conversion from string (for backward compatibility)
    /// </summary>
    public static implicit operator CosmosDbPartitionKey(string partitionKey)
    {
        return new CosmosDbPartitionKey(new PartitionKeyValue(partitionKey));
    }

    /// <summary>
    /// Creates a hierarchical key from a delimited string.
    /// Maximum 3 levels supported.
    /// </summary>
    public static CosmosDbPartitionKey FromDelimited(string delimitedKey, char delimiter = '/')
    {
        if (string.IsNullOrWhiteSpace(delimitedKey))
            throw new ArgumentException("Delimited key cannot be null or empty", nameof(delimitedKey));

        var parts = delimitedKey.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length > MaxLevels)
        {
            throw new ArgumentException($"Azure Cosmos DB supports a maximum of {MaxLevels} hierarchical partition key levels. Delimited key has {parts.Length} parts.", nameof(delimitedKey));
        }

        var values = parts.Select(p => new PartitionKeyValue(p)).ToArray();
        return new CosmosDbPartitionKey(values);
    }
}

/// <summary>
/// Represents a strongly-typed partition key value.
/// Supports Cosmos DB partition key types: string, int, double, bool
/// </summary>
public readonly struct PartitionKeyValue
{
    private readonly PartitionKeyType _type;
    private readonly string? _stringValue;
    private readonly int _intValue;
    private readonly double _doubleValue;
    private readonly bool _boolValue;

    public PartitionKeyValue(string value)
    {
        _type = PartitionKeyType.String;
        _stringValue = value ?? throw new ArgumentNullException(nameof(value));
    }

    public PartitionKeyValue(int value)
    {
        _type = PartitionKeyType.Int;
        _intValue = value;
    }

    public PartitionKeyValue(double value)
    {
        _type = PartitionKeyType.Double;
        _doubleValue = value;
    }

    public PartitionKeyValue(bool value)
    {
        _type = PartitionKeyType.Bool;
        _boolValue = value;
    }

    // ============================================
    // IMPLICIT CONVERSION OPERATORS
    // ============================================

    /// <summary>
    /// Implicitly converts string to PartitionKeyValue
    /// </summary>
    public static implicit operator PartitionKeyValue(string value) => new(value);

    /// <summary>
    /// Implicitly converts int to PartitionKeyValue
    /// </summary>
    public static implicit operator PartitionKeyValue(int value) => new(value);

    /// <summary>
    /// Implicitly converts double to PartitionKeyValue
    /// </summary>
    public static implicit operator PartitionKeyValue(double value) => new(value);

    /// <summary>
    /// Implicitly converts bool to PartitionKeyValue
    /// </summary>
    public static implicit operator PartitionKeyValue(bool value) => new(value);

    // ============================================
    // COSMOS DB CONVERSION METHODS
    // ============================================

    public PartitionKey ToPartitionKey()
    {
        return _type switch
        {
            PartitionKeyType.String => new PartitionKey(_stringValue!),
            PartitionKeyType.Int => new PartitionKey(_intValue),
            PartitionKeyType.Double => new PartitionKey(_doubleValue),
            PartitionKeyType.Bool => new PartitionKey(_boolValue),
            _ => throw new InvalidOperationException($"Unsupported partition key type: {_type}")
        };
    }

    public void AddToBuilder(PartitionKeyBuilder builder)
    {
        switch (_type)
        {
            case PartitionKeyType.String:
                builder.Add(_stringValue!);
                break;
            case PartitionKeyType.Int:
                builder.Add(_intValue);
                break;
            case PartitionKeyType.Double:
                builder.Add(_doubleValue);
                break;
            case PartitionKeyType.Bool:
                builder.Add(_boolValue);
                break;
            default:
                throw new InvalidOperationException($"Unsupported partition key type: {_type}");
        }
    }

    public override string ToString()
    {
        return _type switch
        {
            PartitionKeyType.String => _stringValue!,
            PartitionKeyType.Int => _intValue.ToString(),
            PartitionKeyType.Double => _doubleValue.ToString(),
            PartitionKeyType.Bool => _boolValue.ToString(),
            _ => string.Empty
        };
    }

    private enum PartitionKeyType
    {
        String,
        Int,
        Double,
        Bool
    }
}