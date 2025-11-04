using Clear.DataService.Entities.Cosmos;
using Microsoft.Azure.Cosmos;

namespace Clear.DataService.Models;

public sealed record ContainerBatchBuffer(Container Container,
    Dictionary<string, List<ICosmosDbDocument>> PartitionedItems)
    : IContainerBatchBuffer
{
    public ContainerBatchBuffer(Container container) : this(container, [])
    {
    }

    public IEnumerable<string> GetPartitionKeys() => PartitionedItems.Keys;

    public IEnumerable<ICosmosDbDocument> GetDocuments(string partitionKey)
    {
        return PartitionedItems.TryGetValue(partitionKey, out var list) ? list : Enumerable.Empty<ICosmosDbDocument>();
    }

    public void AddDocument(string partitionKey, params IEnumerable<ICosmosDbDocument> item)
    {
        if (!PartitionedItems.TryGetValue(partitionKey, out var list))
        {
            list = [];
            PartitionedItems[partitionKey] = list;
        }

        list.AddRange(item);
    }
}
