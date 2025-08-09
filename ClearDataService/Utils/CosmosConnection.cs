using System.Data.Common;

namespace Clear.DataService.Utils;

public record CosmosConnection
{
    public CosmosConnection(string connectionString)
    {
        ConnectionString = connectionString;

        var conn = new DbConnectionStringBuilder
        {
            ConnectionString = connectionString
        };

        Endpoint = conn.TryGetValue("AccountEndpoint", out object? endpoint)
            ? endpoint.ToString() ?? "" : throw new ArgumentException(connectionString, nameof(connectionString));

        Key = conn.TryGetValue("AccountKey", out object? key)
            ? key.ToString() ?? "" : throw new ArgumentException(connectionString, nameof(connectionString));
    }

    public CosmosConnection(string endPoint, string key)
    {
        Endpoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
        Key = key ?? throw new ArgumentNullException(nameof(key));
        ConnectionString = $"AccountEndpoint={Endpoint};AccountKey={Key};";
    }

    public string ConnectionString { get; }
    public string Endpoint { get; }
    public string Key { get; }
}