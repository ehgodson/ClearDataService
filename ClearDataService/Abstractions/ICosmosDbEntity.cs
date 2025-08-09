namespace ClearDataService.Abstractions;

public interface ICosmosDbEntity : IBaseEntity
{
    string Id { get; set; }
}
