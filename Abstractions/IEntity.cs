namespace ClearDataService.Abstractions;

public interface IEntity<out T> where T : struct
{
    T Id { get; }
}

public interface IEntity : IEntity<int>
{
}