using Clear.DataService.Abstractions;
using Newtonsoft.Json;

namespace Clear.DataService.Entities.Cosmos;

public class CosmosDbDocument<T> : ICosmosDbDocument where T : ICosmosDbEntity
{
    [JsonProperty("id")]
    public string Id { get; private set; } = default!;

    public string EntityType { get; set; } = default!;

    [JsonProperty("partitionKey")]
    public string PartitionKey { get; private set; } = default!;

    public T Data { get; private set; } = default!;

    [JsonProperty("_etag")]
    public string ETag { get; private set; } = default!;

    [JsonProperty("_rid")]
    public string ResourceId { get; private set; } = default!;

    [JsonProperty("_self")]
    public string SelfLink { get; private set; } = default!;

    [JsonProperty("_attachments")]
    public string Attachments { get; private set; } = default!;

    [JsonProperty("_ts")]
    public long TimestampSeconds { get; private set; }

    public DateTime Timestamp { get; private set; }

    [JsonConstructor]
    private CosmosDbDocument()
    { }

    private CosmosDbDocument(string id, string partitionKey, T data)
    {
        Id = id;
        PartitionKey = partitionKey;
        Data = data;
        Timestamp = DateTime.UtcNow;
        EntityType = typeof(T).Name;
    }

    public static CosmosDbDocument<T> Create(T data, string partitionKey)
    {
        if (string.IsNullOrWhiteSpace(partitionKey))
            throw new ArgumentException("PartitionKey cannot be null or empty.", nameof(partitionKey));

        if (data is null)
            throw new ArgumentNullException(nameof(data), "Data cannot be null.");

        return new CosmosDbDocument<T>(data.Id, partitionKey, data);
    }
}

public interface ICosmosDbDocument
{
    string Id { get; }
    string PartitionKey { get; }
    string ETag { get; }
    DateTime Timestamp { get; }
}