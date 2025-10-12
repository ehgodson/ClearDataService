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
/// Represents a single sort criteria
/// </summary>
/// <typeparam name="T">The type to sort</typeparam>
public class SortCriteria<T>
{
    public Expression<Func<T, object>> KeySelector { get; }
    public SortDirection Direction { get; }

    public SortCriteria(Expression<Func<T, object>> keySelector, SortDirection direction)
    {
        KeySelector = keySelector;
        Direction = direction;
    }
}

/// <summary>
/// A builder for constructing multiple sort criteria for ordering data
/// </summary>
/// <typeparam name="T">The type to build sort criteria for</typeparam>
public class SortBuilder<T>
{
    private readonly List<SortCriteria<T>> _sortCriteria;

    /// <summary>
    /// Creates a new SortBuilder with no sorting criteria
    /// </summary>
    public static SortBuilder<T> Create() => new();

    /// <summary>
    /// Creates a new SortBuilder starting with the specified sort criteria
    /// </summary>
    public static SortBuilder<T> New(Expression<Func<T, object>> keySelector, SortDirection direction = SortDirection.Ascending)
        => new SortBuilder<T>().ThenBy(keySelector, direction);

    private SortBuilder()
    {
        _sortCriteria = new List<SortCriteria<T>>();
    }

    /// <summary>
    /// Adds an ascending sort by the specified property
    /// </summary>
    /// <param name="keySelector">Expression to select the property to sort by</param>
    public SortBuilder<T> ThenBy(Expression<Func<T, object>> keySelector)
    {
        _sortCriteria.Add(new SortCriteria<T>(keySelector, SortDirection.Ascending));
        return this;
    }

    /// <summary>
    /// Adds a descending sort by the specified property
    /// </summary>
    /// <param name="keySelector">Expression to select the property to sort by</param>
    public SortBuilder<T> ThenByDescending(Expression<Func<T, object>> keySelector)
    {
        _sortCriteria.Add(new SortCriteria<T>(keySelector, SortDirection.Descending));
        return this;
    }

    /// <summary>
    /// Adds a sort by the specified property with explicit direction
    /// </summary>
    /// <param name="keySelector">Expression to select the property to sort by</param>
    /// <param name="direction">The sort direction</param>
    public SortBuilder<T> ThenBy(Expression<Func<T, object>> keySelector, SortDirection direction)
    {
        _sortCriteria.Add(new SortCriteria<T>(keySelector, direction));
        return this;
    }

    /// <summary>
    /// Conditionally adds an ascending sort by the specified property
    /// </summary>
    /// <param name="condition">If true, the sort criteria will be added</param>
    /// <param name="keySelector">Expression to select the property to sort by</param>
    public SortBuilder<T> ThenBy(bool condition, Expression<Func<T, object>> keySelector)
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
    public SortBuilder<T> ThenByDescending(bool condition, Expression<Func<T, object>> keySelector)
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
    public SortBuilder<T> ThenBy(bool condition, Expression<Func<T, object>> keySelector, SortDirection direction)
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
    /// Applies the sort criteria to an IQueryable
    /// </summary>
    /// <param name="query">The query to apply sorting to</param>
    /// <returns>The sorted query</returns>
    public IOrderedQueryable<T> ApplyTo(IQueryable<T> query)
    {
        if (!HasSortCriteria)
        {
            // If no sort criteria, return a dummy ordered queryable to maintain the interface
            return query.OrderBy(x => 1);
        }

        IOrderedQueryable<T>? orderedQuery = null;

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
            return $"c.{propertyName} {direction}";
        });

        return $"ORDER BY {string.Join(", ", orderByClauses)}";
    }

    /// <summary>
    /// Extracts property name from expression
    /// </summary>
    private static string GetPropertyName(Expression<Func<T, object>> expression)
    {
        return expression.Body switch
        {
            MemberExpression memberExpr => memberExpr.Member.Name,
            UnaryExpression unaryExpr when unaryExpr.Operand is MemberExpression memberExpr2 => memberExpr2.Member.Name,
            _ => throw new ArgumentException("Expression must be a property accessor", nameof(expression))
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