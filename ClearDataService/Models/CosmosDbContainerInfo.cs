namespace ClearDataService.Models;

public record CosmosDbContainerInfo(
    string Name,
    string PartitionKeyPath
)
{
    public const string DefaultPartitionKeyPath = "/partitionKey";
    public CosmosDbContainerInfo(string name) : this(name, DefaultPartitionKeyPath)
    { }

    public static implicit operator CosmosDbContainerInfo(string name)
    {
        return new CosmosDbContainerInfo(name);
    }
}
