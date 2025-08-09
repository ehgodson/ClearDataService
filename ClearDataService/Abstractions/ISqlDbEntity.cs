namespace ClearDataService.Abstractions;

public interface ISqlDbEntity<out T> : IBaseEntity
{
    T Id { get; }
}

public interface ISqlDbEntity : ISqlDbEntity<int>
{ }