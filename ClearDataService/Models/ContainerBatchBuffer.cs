using Clear.DataService.Entities.Cosmos;
using Microsoft.Azure.Cosmos;

namespace Clear.DataService.Models;

/// <summary>
/// Base interface for container batch buffers to allow storing different generic types in a single dictionary
/// </summary>
public interface IContainerBatchBuffer
{
    Container Container { get; }
    Dictionary<string, CosmosDbDocKeyCombo> PartitionedItems { get; }
}


public sealed record ContainerBatchBuffer(Container Container,
    Dictionary<string, CosmosDbDocKeyCombo> PartitionedItems)
    : IContainerBatchBuffer
{
    public ContainerBatchBuffer(Container container) : this(container, [])
    {
    }

    public IEnumerable<string> GetPartitionKeys() => PartitionedItems.Keys;

    public CosmosDbDocKeyCombo GetDocuments(CosmosDbPartitionKey partitionKey)
    {
        return PartitionedItems.TryGetValue(partitionKey.GetKey(), out var list) 
            ? list : CosmosDbDocKeyCombo.Create(partitionKey);
    }

    public void AddDocument(CosmosDbPartitionKey partitionKey, params IEnumerable<ICosmosDbDocument> item)
    {
        var key = partitionKey.GetKey();

        if (!PartitionedItems.TryGetValue(key, out var list))
        {
            // Create new list with items - items are already added here
            list = CosmosDbDocKeyCombo.Create(partitionKey, item);
            PartitionedItems[key] = list;
        }
        else
        {
            // List already exists - add items to existing list
            list.AddDocuments(partitionKey, item);
        }
    }
}

public record CosmosDbDocKeyCombo(
    CosmosDbPartitionKey PartitionKey,
    List<ICosmosDbDocument> Documents
)
{
    public static CosmosDbDocKeyCombo Create(
        CosmosDbPartitionKey partitionKey,
        params IEnumerable<ICosmosDbDocument> documents
    )
    {
        return new(partitionKey, documents.ToList()!);
    }

    public void AddDocuments(CosmosDbPartitionKey partitionKey, params IEnumerable<ICosmosDbDocument> documents)
    {
        if (partitionKey.GetKey() != PartitionKey.GetKey())
        {
            throw new ArgumentException("Partition key does not match", nameof(partitionKey));
        }

        Documents.AddRange(documents);
    }
}