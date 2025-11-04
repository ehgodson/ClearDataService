namespace Clear.DataService.Models;

/// <summary>
/// Configuration for Cosmos DB container creation with support for both single and hierarchical partition keys.
/// Azure Cosmos DB supports up to 3 levels of hierarchical partition keys.
/// </summary>
public record CosmosDbContainerInfo
{
    /// <summary>
    /// Default single partition key path
    /// </summary>
    public const string DefaultPartitionKeyPath = "/partitionKey";

    /// <summary>
    /// Creates a container configuration with hierarchical partition keys (1-3 levels).
    /// The primary path is always /partitionKey.
    /// </summary>
    private CosmosDbContainerInfo(string name, string? secondPath = null, string? thirdPath = null)
    {
        Name = name;

        List<string> paths = [DefaultPartitionKeyPath];

        if (!string.IsNullOrWhiteSpace(secondPath))
        {
            ValidatePartitionKeyPath(secondPath, nameof(secondPath));
            paths.Add(NormalizePartitionKeyPath(secondPath));
        }

        if (!string.IsNullOrWhiteSpace(thirdPath))
        {
            ValidatePartitionKeyPath(thirdPath, nameof(thirdPath));
            paths.Add(NormalizePartitionKeyPath(thirdPath));
        }

        PartitionKeyPaths = [.. paths];
        IsHierarchical = paths.Count > 1;
    }

    /// <summary>
    /// Container name
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Partition key paths (single or hierarchical)
    /// </summary>
    public string[] PartitionKeyPaths { get; }

    /// <summary>
    /// Primary partition key path (always /partitionKey)
    /// </summary>
    public string PartitionKeyPath => PartitionKeyPaths[0];

    /// <summary>
    /// Indicates if this container uses hierarchical partition keys
    /// </summary>
    public bool IsHierarchical { get; }

    /// <summary>
    /// Gets the number of partition key levels (1-3)
    /// </summary>
    public int PartitionKeyLevels => PartitionKeyPaths.Length;

    // ============================================
    // FLUENT API - Primary Creation Methods
    // ============================================

    /// <summary>
    /// Creates a container with single partition key (/partitionKey).
    /// Example: CosmosDbContainerInfo.Create("Customers")
    /// </summary>
    public static CosmosDbContainerInfo Create(string name)
    {
        return new CosmosDbContainerInfo(name);
    }

    /// <summary>
    /// Creates a container with the default primary partition key, ready for fluent configuration.
    /// Example: CosmosDbContainerInfo.CreateWithDefaultPK("Customers").AddSecondPKPath("/userId")
    /// </summary>
    public static CosmosDbContainerInfo CreateWithDefaultPK(string name)
    {
        return new CosmosDbContainerInfo(name);
    }

    // ============================================
    // FLUENT API - Path Addition Methods
    // ============================================

    /// <summary>
    /// Adds a second partition key path to create a 2-level hierarchy.
    /// Example: CreateWithDefaultPK("Users").AddSecondPKPath("/userId")
    /// </summary>
    public CosmosDbContainerInfo AddSecondPKPath(string secondPath)
    {
        if (PartitionKeyLevels > 1)
        {
            throw new InvalidOperationException("Second partition key path already configured.");
        }

        return new CosmosDbContainerInfo(Name, secondPath);
    }

    /// <summary>
    /// Adds a third partition key path to create a 3-level hierarchy.
    /// Must be called after AddSecondPKPath().
    /// Example: CreateWithDefaultPK("Orders").AddSecondPKPath("/customerId").AddThirdPKPath("/orderId")
    /// </summary>
    public CosmosDbContainerInfo AddThirdPKPath(string thirdPath)
    {
        if (PartitionKeyLevels < 2)
        {
            throw new InvalidOperationException("Must add second partition key path before adding third path.");
        }

        if (PartitionKeyLevels > 2)
        {
            throw new InvalidOperationException("Third partition key path already configured.");
        }

        return new CosmosDbContainerInfo(Name, PartitionKeyPaths[1], thirdPath);
    }

    // ============================================
    // LEGACY API - Preserved for Backward Compatibility
    // ============================================

    /// <summary>
    /// Implicit conversion from string for single partition key containers
    /// </summary>
    public static implicit operator CosmosDbContainerInfo(string name)
    {
        return new CosmosDbContainerInfo(name);
    }

    /// <summary>
    /// Creates a container with single partition key (/partitionKey)
    /// </summary>
    public static CosmosDbContainerInfo CreateSingle(string name)
    {
        return new CosmosDbContainerInfo(name);
    }

    /// <summary>
    /// Creates a container with 2-level hierarchical partition keys.
    /// Level 1: /partitionKey (default)
    /// Level 2: secondPath
    /// Example: CreateHierarchical("Users", "/userId")
    /// </summary>
    public static CosmosDbContainerInfo CreateHierarchical(string name, string secondPath)
    {
        return new CosmosDbContainerInfo(name, secondPath);
    }

    /// <summary>
    /// Creates a container with 3-level hierarchical partition keys.
    /// Level 1: /partitionKey (default)
    /// Level 2: secondPath
    /// Level 3: thirdPath
    /// Example: CreateHierarchical("Orders", "/customerId", "/orderId")
    /// </summary>
    public static CosmosDbContainerInfo CreateHierarchical(string name, string secondPath, string thirdPath)
    {
        return new CosmosDbContainerInfo(name, secondPath, thirdPath);
    }

    /// <summary>
    /// Creates a 2-level hierarchical container for multi-tenant scenarios.
    /// Level 1: /partitionKey (stores tenantId)
    /// Level 2: subPath (e.g., "/userId")
    /// Example: CreateMultiTenant("Users", "/userId")
    /// </summary>
    public static CosmosDbContainerInfo CreateMultiTenant(string name, string subPath)
    {
        return new CosmosDbContainerInfo(name, subPath);
    }

    /// <summary>
    /// Creates a 3-level hierarchical container for multi-tenant scenarios.
    /// Level 1: /partitionKey (stores tenantId)
    /// Level 2: secondPath (e.g., "/userId")
    /// Level 3: thirdPath (e.g., "/documentId")
    /// Example: CreateMultiTenant("Documents", "/folderId", "/documentId")
    /// </summary>
    public static CosmosDbContainerInfo CreateMultiTenant(string name, string secondPath, string thirdPath)
    {
        return new CosmosDbContainerInfo(name, secondPath, thirdPath);
    }

    // ============================================
    // VALIDATION & NORMALIZATION
    // ============================================

    /// <summary>
    /// Validates a partition key path
    /// </summary>
    private static void ValidatePartitionKeyPath(string path, string paramName)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Partition key path cannot be null or empty", paramName);
        }

        var normalizedPath = NormalizePartitionKeyPath(path);
        if (normalizedPath == DefaultPartitionKeyPath)
        {
            throw new ArgumentException($"Cannot use '{DefaultPartitionKeyPath}' as additional path. This is reserved for the primary partition key.", paramName);
        }
    }

    /// <summary>
    /// Normalizes a partition key path by ensuring it starts with '/'
    /// </summary>
    private static string NormalizePartitionKeyPath(string path)
    {
        return path.StartsWith('/') ? path : $"/{path}";
    }
}
