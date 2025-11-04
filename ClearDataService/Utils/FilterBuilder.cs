using Clear.DataService.Abstractions;
using Clear.DataService.Entities.Cosmos;

namespace Clear.DataService.Utils;

/// <summary>
/// A filter builder for CosmosDbDocument types that ensures all filtering happens at the database level.
/// Provides implicit conversions to Expression types for seamless integration with the data access layer.
/// </summary>
/// <typeparam name="T">The entity type that implements ICosmosDbEntity</typeparam>
public class FilterBuilder<T> where T : ICosmosDbEntity
{
    private Expression<Func<CosmosDbDocument<T>, bool>>? _predicate;

    /// <summary>
    /// Creates a new FilterBuilder with no filters (matches all)
    /// </summary>
    public static FilterBuilder<T> Create() => new();

    /// <summary>
    /// Creates a new FilterBuilder starting with a document-level predicate
    /// </summary>
    public static FilterBuilder<T> New(Expression<Func<CosmosDbDocument<T>, bool>> predicate) => new(predicate);

    private FilterBuilder()
    {
        _predicate = null;
    }

    private FilterBuilder(Expression<Func<CosmosDbDocument<T>, bool>> predicate)
    {
        _predicate = predicate;
    }

    /// <summary>
    /// Conditionally adds a predicate using AND logic
    /// </summary>
    /// <param name="predicate">The predicate to add</param>
    /// <param name="condition">If true, the predicate will be added (default: true)</param>
    public FilterBuilder<T> And(Expression<Func<CosmosDbDocument<T>, bool>> predicate, bool condition = true)
    {
        if (condition)
        {
            _predicate = _predicate?.And(predicate) ?? predicate;
        }
        return this;
    }

    /// <summary>
    /// Conditionally adds a predicate using OR logic
    /// </summary>
    /// <param name="predicate">The predicate to add</param>
    /// <param name="condition">If true, the predicate will be added (default: true)</param>
    public FilterBuilder<T> Or(Expression<Func<CosmosDbDocument<T>, bool>> predicate, bool condition = true)
    {
        if (condition)
        {
            _predicate = _predicate?.Or(predicate) ?? predicate;
        }
        return this;
    }

    /// <summary>
    /// Returns true if any predicates have been added
    /// </summary>
    public bool HasFilters => _predicate != null;

    /// <summary>
    /// Builds the final predicate. Returns a "true" predicate if no conditions were added.
    /// </summary>
    public Expression<Func<CosmosDbDocument<T>, bool>> Build()
    {
        return _predicate ?? (doc => true);
    }

    /// <summary>
    /// Implicit conversion to Expression for database queries
    /// </summary>
    public static implicit operator Expression<Func<CosmosDbDocument<T>, bool>>(FilterBuilder<T> builder)
    {
        return builder.Build();
    }

    /// <summary>
    /// Implicit conversion from Expression (for convenience)
    /// </summary>
    public static implicit operator FilterBuilder<T>(Expression<Func<CosmosDbDocument<T>, bool>> predicate)
    {
        return new FilterBuilder<T>(predicate);
    }
}