using Microsoft.Azure.Cosmos;

namespace ClearDataService.Models;

public sealed record ContainerBatchBuffer(Container Container, Dictionary<string, List<object>> PartitionedItems)
{
    public ContainerBatchBuffer(Container container) : this(container, [])
    {
    }

    public void AddItem(string partitionKey, object item)
    {
        if (!PartitionedItems.TryGetValue(partitionKey, out var list))
        {
            list = [];
            PartitionedItems[partitionKey] = list;
        }

        list.Add(item);
    }
}
