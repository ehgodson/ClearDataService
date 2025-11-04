using Clear.DataService.Entities.Cosmos;
using Microsoft.Azure.Cosmos;

namespace Clear.DataService.Models;

/// <summary>
/// Base interface for container batch buffers to allow storing different generic types in a single dictionary
/// </summary>
public interface IContainerBatchBuffer
{
    Container Container { get; }
    Dictionary<string, List<ICosmosDbDocument>> PartitionedItems { get; }
}
