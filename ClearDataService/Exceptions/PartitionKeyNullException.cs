namespace ClearDataService.Exceptions;

public class PartitionKeyNullException : BaseException
{
    public PartitionKeyNullException() : base("Partition key cannot be null or empty.")
    { }
}