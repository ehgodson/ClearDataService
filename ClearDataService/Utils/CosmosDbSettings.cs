namespace Clear.DataService.Utils;

public interface ICosmosDbSettings
{
    string EndpointUri { get; }
    string PrimaryKey { get; }
    string DatabaseName { get; }
}

public record CosmosDbSettings : ICosmosDbSettings
{
    protected CosmosDbSettings(string endpointUri, string primaryKey, string databaseName)
    {
        EndpointUri = endpointUri;
        PrimaryKey = primaryKey;
        DatabaseName = databaseName;
    }

    public string EndpointUri { get; private set; } = null!;
    public string PrimaryKey { get; private set; } = null!;
    public string DatabaseName { get; private set; } = null!;

    public static CosmosDbSettings Create(string endpointUri, string primaryKey, string databaseName)
    {
        if (string.IsNullOrWhiteSpace(endpointUri))
            throw new ArgumentException("Endpoint URI cannot be null or empty.", nameof(endpointUri));
        if (string.IsNullOrWhiteSpace(primaryKey))
            throw new ArgumentException("Primary key cannot be null or empty.", nameof(primaryKey));
        if (string.IsNullOrWhiteSpace(databaseName))
            throw new ArgumentException("Database name cannot be null or empty.", nameof(databaseName));
        return new CosmosDbSettings(endpointUri, primaryKey, databaseName);
    }
}