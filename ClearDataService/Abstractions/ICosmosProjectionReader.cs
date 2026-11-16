using Clear.DataService.Models;
using Microsoft.Azure.Cosmos;

namespace Clear.DataService.Abstractions;

/// <summary>Reads a single page of a projected Cosmos SQL query in one explicit partition.</summary>
public interface ICosmosProjectionReader
{
    Task<PagedResult<T>> ReadAsync<T>(string containerName, QueryDefinition query, PartitionKey partitionKey,
        int pageSize, string? continuationToken = null, CancellationToken cancellationToken = default);
}
