using System.Net;

namespace ClearDataService.Models;

public record CosmosBatchResult
{
    public CosmosBatchResult(bool successful, string containerName,
        string partitionKey, HttpStatusCode? statusCode, string? message)
    {
        Successful = successful;
        ContainerName = containerName;
        PartitionKey = partitionKey;
        StatusCode = statusCode;
        Message = message;
    }

    public bool Successful { get; }
    public string ContainerName { get; }
    public string PartitionKey { get; }
    public HttpStatusCode? StatusCode { get; }
    public string? Message { get; }

    public static CosmosBatchResult Success(string containerName,
        string partitionKey, HttpStatusCode statusCode, string message = "")
    => new(true, containerName, partitionKey, statusCode, string.Empty);

    public static CosmosBatchResult Failure(string containerName,
        string partitionKey, HttpStatusCode? statusCode, string message = "")
    => new(false, containerName, partitionKey, statusCode, message);

    public override string ToString()
    {
        return
            $"[Container: {ContainerName}, PartitionKey: {PartitionKey}] " +
            $"{(Successful ? "succeeds" : "failed")}" +
            $"{(StatusCode == null ? "" : $" with status {StatusCode}")} " +
            $"{Message}".Trim();
    }
}