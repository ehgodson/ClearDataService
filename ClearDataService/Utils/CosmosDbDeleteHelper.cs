using Clear.DataService.Models;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;
using System.Net;

namespace Clear.DataService.Utils;

/// <summary>
/// Helper class for Cosmos DB delete operations with support for hierarchical partition keys
/// </summary>
internal static class CosmosDbDeleteHelper
{
    /// <summary>
    /// Builds a SQL SELECT clause that includes the document ID and all partition key paths
    /// </summary>
    /// <param name="partitionKeyPaths">Array of partition key paths (e.g., ["/partitionKey", "/userId"])</param>
    /// <returns>SQL SELECT clause</returns>
    public static string BuildSelectClause(string[] partitionKeyPaths)
    {
        var selectFields = new List<string> { "c.id" };
        
        foreach (var path in partitionKeyPaths)
        {
            // Remove leading '/' and convert to property access
            var fieldName = path.Replace('/', '.');
            selectFields.Add($"c{fieldName}");
        }

        return $"SELECT {string.Join(", ", selectFields)} FROM c";
    }

    /// <summary>
    /// Builds a PartitionKey from a dynamic item by extracting values from the specified partition key paths
    /// </summary>
    /// <param name="item">Dynamic item (typically a JObject from Cosmos DB query)</param>
    /// <param name="partitionKeyPaths">Array of partition key paths</param>
    /// <returns>Constructed PartitionKey (single or hierarchical)</returns>
    public static PartitionKey BuildPartitionKeyFromItem(dynamic item, string[] partitionKeyPaths)
    {
        if (partitionKeyPaths.Length == 1)
        {
            // Single partition key
            var path = partitionKeyPaths[0].TrimStart('/');
            var value = GetDynamicPropertyValue(item, path);
            
            if (string.IsNullOrEmpty(value))
            {
                throw new InvalidOperationException($"Partition key value for path '{path}' is null or empty");
            }
            
            return new PartitionKey(value);
        }
        else
        {
            // Hierarchical partition key - use PartitionKeyBuilder
            var builder = new PartitionKeyBuilder();
            
            foreach (var path in partitionKeyPaths)
            {
                var fieldName = path.TrimStart('/');
                var value = GetDynamicPropertyValue(item, fieldName);
                
                if (string.IsNullOrEmpty(value))
                {
                    throw new InvalidOperationException($"Partition key value for path '{path}' is null or empty");
                }
                
                builder.Add(value);
            }
            
            return builder.Build();
        }
    }

    /// <summary>
    /// Safely deletes a single item from Cosmos DB with error handling
    /// </summary>
    /// <param name="container">Cosmos DB container</param>
    /// <param name="itemId">Document ID to delete</param>
    /// <param name="partitionKey">Partition key for the document</param>
    /// <returns>Tuple with success status and error message (if any)</returns>
    public static async Task<(string id, bool success, string? error)> DeleteItemSafeAsync(
        Container container,
        string itemId,
        PartitionKey partitionKey)
    {
        try
        {
            await container.DeleteItemAsync<dynamic>(
                id: itemId,
                partitionKey: partitionKey
            );
            return (itemId, true, null);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // Item already deleted - treat as success
            return (itemId, true, null);
        }
        catch (CosmosException ex)
        {
            // Capture detailed Cosmos DB error information
            var errorMsg = $"CosmosException: StatusCode={ex.StatusCode}, SubStatusCode={ex.SubStatusCode}, Message={ex.Message}";
            return (itemId, false, errorMsg);
        }
        catch (Exception ex)
        {
            return (itemId, false, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Processes batch delete results and updates success/failure counters
    /// </summary>
    /// <param name="results">Array of delete results</param>
    /// <param name="successCount">Reference to success counter</param>
    /// <param name="failureCount">Reference to failure counter</param>
    /// <param name="failedDeletes">List to collect failed delete details</param>
    public static void ProcessResults(
        (string id, bool success, string? error)[] results,
        ref int successCount,
        ref int failureCount,
        List<string> failedDeletes)
    {
        foreach (var result in results)
        {
            if (result.success)
            {
                successCount++;
            }
            else
            {
                failureCount++;
                failedDeletes.Add($"{result.id} ({result.error})");
            }
        }
    }

    /// <summary>
    /// Extracts a property value from a dynamic object (handles JObject and dictionaries)
    /// </summary>
    /// <param name="item">Dynamic item (typically JObject from Cosmos DB)</param>
    /// <param name="propertyName">Property name to extract</param>
    /// <returns>String value of the property, or empty string if not found</returns>
    private static string GetDynamicPropertyValue(dynamic item, string propertyName)
    {
        try
        {
            // Handle Newtonsoft.Json.Linq.JObject (returned by Cosmos DB)
            if (item is JObject jObject)
            {
                var token = jObject[propertyName];
                return token?.ToString() ?? string.Empty;
            }
            
            // Fallback to dictionary access
            var props = (IDictionary<string, object>)item;
            var property = props[propertyName];
            return property?.ToString() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
