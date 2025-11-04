namespace Clear.DataService.Abstractions;

public interface ICosmosDbEntity : IBaseEntity
{
    string Id { get; set; }
}
