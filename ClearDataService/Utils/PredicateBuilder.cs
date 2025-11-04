using Clear.DataService.Abstractions;
using Clear.DataService.Entities.Cosmos;

namespace Clear.DataService.Utils;

/// <summary>
/// A generic predicate builder for dynamically constructing filters based on conditions.
/// Allows building complex WHERE clauses by conditionally adding predicates.
/// </summary>
/// <typeparam name="T">The type to build predicates for</typeparam>
public class PredicateBuilder<T> where T : ICosmosDbEntity
{
    private Expression<Func<CosmosDbDocument<T>, bool>>? _predicate;

    /// <summary>
    /// Creates a new PredicateBuilder starting with a "true" predicate (no filtering)
    /// </summary>
    public static PredicateBuilder<T> Create() => new();

    /// <summary>
    /// Creates a new PredicateBuilder starting with the specified predicate
    /// </summary>
    public static PredicateBuilder<T> New(Expression<Func<CosmosDbDocument<T>, bool>> predicate) => new(predicate);

    private PredicateBuilder()
    {
        _predicate = null; // Start with no predicate (will default to "true")
    }

    private PredicateBuilder(Expression<Func<CosmosDbDocument<T>, bool>> predicate)
    {
        _predicate = predicate;
    }

    /// <summary>
    /// Conditionally adds a predicate using AND logic
    /// </summary>
    /// <param name="predicate">The predicate to add if condition is true</param>
    /// <param name="condition">If true, the predicate will be added</param>
    public PredicateBuilder<T> And(Expression<Func<CosmosDbDocument<T>, bool>> predicate, bool condition = true)
    {
        if (condition)
        {
            _predicate = _predicate == null ? predicate : _predicate.And(predicate);
        }
        return this;
    }

    /// <summary>
    /// Conditionally adds a predicate using OR logic
    /// </summary>
    /// <param name="predicate">The predicate to add if condition is true</param>
    /// <param name="condition">If true, the predicate will be added</param>
    public PredicateBuilder<T> Or(Expression<Func<CosmosDbDocument<T>, bool>> predicate, bool condition = true)
    {
        if (condition)
        {
            _predicate = _predicate == null ? predicate : _predicate.Or(predicate);
        }
        return this;
    }

    /// <summary>
    /// Builds and returns the final predicate. Returns a "true" predicate if no conditions were added.
    /// </summary>
    public Expression<Func<CosmosDbDocument<T>, bool>> Build()
    {
        return _predicate ?? (x => true); // Default to "true" if no predicates added
    }

    /// <summary>
    /// Returns true if any predicates have been added
    /// </summary>
    public bool HasPredicates => _predicate != null;

    ///// <summary>
    ///// Implicit conversion to Expression for convenience
    ///// </summary>
    //public static implicit operator Expression<Func<T, bool>>(PredicateBuilder<T> builder)
    //{
    //    return builder.Build();
    //}
}

/// <summary>
/// Extension methods for combining Expression predicates
/// </summary>
public static class ExpressionExtensions
{
    /// <summary>
    /// Combines two expressions with AND logic
    /// </summary>
    public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var leftVisitor = new ParameterReplaceVisitor(left.Parameters[0], parameter);
        var rightVisitor = new ParameterReplaceVisitor(right.Parameters[0], parameter);

        var leftBody = leftVisitor.Visit(left.Body);
        var rightBody = rightVisitor.Visit(right.Body);

        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(leftBody, rightBody), parameter);
    }

    /// <summary>
    /// Combines two expressions with OR logic
    /// </summary>
    public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var leftVisitor = new ParameterReplaceVisitor(left.Parameters[0], parameter);
        var rightVisitor = new ParameterReplaceVisitor(right.Parameters[0], parameter);

        var leftBody = leftVisitor.Visit(left.Body);
        var rightBody = rightVisitor.Visit(right.Body);

        return Expression.Lambda<Func<T, bool>>(Expression.OrElse(leftBody, rightBody), parameter);
    }

    /// <summary>
    /// Negates an expression (NOT logic)
    /// </summary>
    public static Expression<Func<T, bool>> Not<T>(this Expression<Func<T, bool>> expression)
    {
        var parameter = expression.Parameters[0];
        var body = Expression.Not(expression.Body);
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }
}

/// <summary>
/// Helper class for replacing parameter references in expressions
/// </summary>
internal class ParameterReplaceVisitor : ExpressionVisitor
{
    private readonly ParameterExpression _from;
    private readonly ParameterExpression _to;

    public ParameterReplaceVisitor(ParameterExpression from, ParameterExpression to)
    {
        _from = from;
        _to = to;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        return node == _from ? _to : base.VisitParameter(node);
    }
}

//public class FilterBuilder<T>
//{
//    private Expression<Func<T, bool>> _expression = x => true;

//    public FilterBuilder<T> Where(Expression<Func<T, bool>> newFilter)
//    {
//        _expression = _expression.And(newFilter);
//        return this;
//    }

//    public Expression<Func<T, bool>> Build() => _expression;
//}