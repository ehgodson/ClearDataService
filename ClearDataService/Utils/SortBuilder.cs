using Clear.DataService.Abstractions;
using Clear.DataService.Entities.Cosmos;
using System.Linq.Expressions;

namespace Clear.DataService.Utils;

/// <summary>
/// Represents the sort direction for a property
/// </summary>
public enum SortDirection
{
    Ascending,
    Descending
}

/// <summary>
/// Represents a single sort criteria for Cosmos DB documents
/// </summary>
/// <typeparam name="T">The entity type that implements ICosmosDbEntity</typeparam>
public class SortCriteria<T> where T : ICosmosDbEntity
{
    public Expression<Func<CosmosDbDocument<T>, object>> KeySelector { get; }
    public SortDirection Direction { get; }

    public SortCriteria(Expression<Func<CosmosDbDocument<T>, object>> keySelector, SortDirection direction)
    {
        KeySelector = keySelector;
        Direction = direction;
    }
}

/// <summary>
/// A builder for constructing multiple sort criteria for ordering Cosmos DB documents.
/// Works directly with CosmosDbDocument<T> to match FilterBuilder<T> pattern.
/// </summary>
/// <typeparam name="T">The entity type that implements ICosmosDbEntity</typeparam>
public class SortBuilder<T> where T : ICosmosDbEntity
{
    private readonly List<SortCriteria<T>> _sortCriteria;

    /// <summary>
    /// Creates a new SortBuilder with no sorting criteria
    /// </summary>
    public static SortBuilder<T> Create() => new();

    /// <summary>
    /// Creates a new SortBuilder starting with the specified sort criteria
    /// </summary>
    public static SortBuilder<T> New(Expression<Func<CosmosDbDocument<T>, object>> keySelector, SortDirection direction = SortDirection.Ascending)
     => new SortBuilder<T>().ThenBy(keySelector, direction);

    private SortBuilder()
    {
        _sortCriteria = new List<SortCriteria<T>>();
    }

    /// <summary>
    /// Adds an ascending sort by the specified property
    /// </summary>
    /// <param name="keySelector">Expression to select the property to sort by</param>
    public SortBuilder<T> ThenBy(Expression<Func<CosmosDbDocument<T>, object>> keySelector)
    {
        _sortCriteria.Add(new SortCriteria<T>(keySelector, SortDirection.Ascending));
        return this;
    }

    /// <summary>
    /// Adds a descending sort by the specified property
    /// </summary>
    /// <param name="keySelector">Expression to select the property to sort by</param>
    public SortBuilder<T> ThenByDescending(Expression<Func<CosmosDbDocument<T>, object>> keySelector)
    {
        _sortCriteria.Add(new SortCriteria<T>(keySelector, SortDirection.Descending));
        return this;
    }

    /// <summary>
    /// Adds a sort by the specified property with explicit direction
    /// </summary>
    /// <param name="keySelector">Expression to select the property to sort by</param>
    /// <param name="direction">The sort direction</param>
    public SortBuilder<T> ThenBy(Expression<Func<CosmosDbDocument<T>, object>> keySelector, SortDirection direction)
    {
        _sortCriteria.Add(new SortCriteria<T>(keySelector, direction));
        return this;
    }

    /// <summary>
    /// Conditionally adds an ascending sort by the specified property
    /// </summary>
    /// <param name="condition">If true, the sort criteria will be added</param>
    /// <param name="keySelector">Expression to select the property to sort by</param>
    public SortBuilder<T> ThenBy(bool condition, Expression<Func<CosmosDbDocument<T>, object>> keySelector)
    {
        if (condition)
        {
            ThenBy(keySelector);
        }
        return this;
    }

    /// <summary>
    /// Conditionally adds a descending sort by the specified property
    /// </summary>
    /// <param name="condition">If true, the sort criteria will be added</param>
    /// <param name="keySelector">Expression to select the property to sort by</param>
    public SortBuilder<T> ThenByDescending(bool condition, Expression<Func<CosmosDbDocument<T>, object>> keySelector)
    {
        if (condition)
        {
            ThenByDescending(keySelector);
        }
        return this;
    }

    /// <summary>
    /// Conditionally adds a sort by the specified property with explicit direction
    /// </summary>
    /// <param name="condition">If true, the sort criteria will be added</param>
    /// <param name="keySelector">Expression to select the property to sort by</param>
    /// <param name="direction">The sort direction</param>
    public SortBuilder<T> ThenBy(bool condition, Expression<Func<CosmosDbDocument<T>, object>> keySelector, SortDirection direction)
    {
        if (condition)
        {
            ThenBy(keySelector, direction);
        }
        return this;
    }

    /// <summary>
    /// Returns true if any sort criteria have been added
    /// </summary>
    public bool HasSortCriteria => _sortCriteria.Count > 0;

    /// <summary>
    /// Gets all the sort criteria
    /// </summary>
    public IReadOnlyList<SortCriteria<T>> SortCriteria => _sortCriteria.AsReadOnly();

    /// <summary>
    /// Applies the sort criteria to an IQueryable of CosmosDbDocument
    /// </summary>
    /// <param name="query">The query to apply sorting to</param>
    /// <returns>The sorted query</returns>
    public IOrderedQueryable<CosmosDbDocument<T>> ApplyTo(IQueryable<CosmosDbDocument<T>> query)
    {
        if (!HasSortCriteria)
        {
            // If no sort criteria, return a dummy ordered queryable to maintain the interface
            return query.OrderBy(x => 1);
        }

        IOrderedQueryable<CosmosDbDocument<T>>? orderedQuery = null;

        for (int i = 0; i < _sortCriteria.Count; i++)
        {
            var criteria = _sortCriteria[i];

            if (i == 0)
            {
                // First sort criteria uses OrderBy/OrderByDescending
                orderedQuery = criteria.Direction == SortDirection.Ascending
      ? query.OrderBy(criteria.KeySelector)
          : query.OrderByDescending(criteria.KeySelector);
            }
            else
            {
                // Subsequent sort criteria use ThenBy/ThenByDescending
                orderedQuery = criteria.Direction == SortDirection.Ascending
                    ? orderedQuery!.ThenBy(criteria.KeySelector)
                       : orderedQuery!.ThenByDescending(criteria.KeySelector);
            }
        }

        return orderedQuery!;
    }

    /// <summary>
    /// Builds the sorting as a SQL ORDER BY clause for use with SQL queries
    /// This is useful for Cosmos DB SQL queries
    /// </summary>
    /// <param name="propertyNameMapper">Optional function to map property names (e.g., for camelCase conversion)</param>
    /// <returns>SQL ORDER BY clause or empty string if no sort criteria</returns>
    public string ToSqlOrderBy(Func<string, string>? propertyNameMapper = null)
    {
        if (!HasSortCriteria)
        {
            return string.Empty;
        }

        var orderByClauses = _sortCriteria.Select(criteria =>
        {
            var propertyName = GetPropertyName(criteria.KeySelector);
            if (propertyNameMapper != null)
            {
                propertyName = propertyNameMapper(propertyName);
            }

            var direction = criteria.Direction == SortDirection.Ascending ? "ASC" : "DESC";
            return $"{propertyName} {direction}";
        });

        return $"ORDER BY {string.Join(", ", orderByClauses)}";
    }

    /// <summary>
    /// Extracts property name from expression.
    /// Handles both doc.Data.Property and direct property access patterns.
    /// </summary>
    private static string GetPropertyName(Expression<Func<CosmosDbDocument<T>, object>> expression)
    {
        return expression.Body switch
        {
            // Direct member access: doc.Data.PropertyName
            MemberExpression memberExpr when memberExpr.Expression is MemberExpression parentMember
             && parentMember.Member.Name == "Data"
           => memberExpr.Member.Name,

            // Unary conversion with member access: Convert(doc.Data.PropertyName)
            UnaryExpression unaryExpr when unaryExpr.Operand is MemberExpression memberExpr2
                   && memberExpr2.Expression is MemberExpression parentMember2
                && parentMember2.Member.Name == "Data"
             => memberExpr2.Member.Name,

            // Direct property without .Data wrapper
            MemberExpression directMember => directMember.Member.Name,

            // Unary with direct property
            UnaryExpression unaryDirect when unaryDirect.Operand is MemberExpression directMember2
                 => directMember2.Member.Name,

            _ => throw new ArgumentException("Expression must be a property accessor (e.g., doc => doc.Data.PropertyName)", nameof(expression))
        };
    }

    /// <summary>
    /// Clears all sort criteria
    /// </summary>
    public SortBuilder<T> Clear()
    {
        _sortCriteria.Clear();
        return this;
    }
}