using ClearDataService.Utils;
using Microsoft.Azure.Cosmos;

namespace ClearDataService.Factory;

public static class CosmosDbClientFactory //: ICosmosDbClientFactory
{
    public static CosmosClient CreateClient(ICosmosDbSettings settings)
    => new(settings.EndpointUri, settings.PrimaryKey);

    public static CosmosClient CreateClient(ICosmosDbSettings settings, CosmosClientOptions cosmosClientOptions)
    => new(settings.EndpointUri, settings.PrimaryKey, cosmosClientOptions);

    public static CosmosClient CreateClientWithBulkOperation(ICosmosDbSettings settings, CosmosClientOptions cosmosClientOptions,
        int maxRetryAttemptsOnRateLimitedRequests, int maxRetryWaitTimeOnRateLimitedRequests)
    {
        cosmosClientOptions.AllowBulkExecution = true;
        cosmosClientOptions.MaxRetryAttemptsOnRateLimitedRequests = maxRetryAttemptsOnRateLimitedRequests;
        cosmosClientOptions.MaxRetryWaitTimeOnRateLimitedRequests = new TimeSpan(maxRetryWaitTimeOnRateLimitedRequests);

        return new(settings.EndpointUri, settings.PrimaryKey, cosmosClientOptions);
    }
}