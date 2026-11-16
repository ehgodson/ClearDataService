using Clear.DataService.Utils;
using Clear.DataService.Abstractions;
using Clear.DataService.Models;
using Microsoft.Azure.Cosmos;

namespace Clear.DataService.Contexts;

internal sealed class CosmosProjectionReader(CosmosClient client, ICosmosDbSettings settings) : ICosmosProjectionReader
{
    public async Task<PagedResult<T>> ReadAsync<T>(string containerName, QueryDefinition query, PartitionKey partitionKey,
        int pageSize, string? continuationToken = null, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);
        using var iterator = client.GetContainer(settings.DatabaseName, containerName).GetItemQueryIterator<T>(
            query, continuationToken, new QueryRequestOptions { PartitionKey = partitionKey, MaxItemCount = pageSize });
        var response = await iterator.ReadNextAsync(cancellationToken);
        return new PagedResult<T>
        {
            Items = response.ToList(),
            Count = response.Count,
            ContinuationToken = response.ContinuationToken,
            HasMoreResults = !string.IsNullOrEmpty(response.ContinuationToken),
            RequestCharge = response.RequestCharge
        };
    }
}

