using Dapper.Npa.Query.Expressions;

namespace Dapper.Npa.Query.Query;
using System.Linq.Expressions;
using Dapper.Npa.Abstractions.Paging;
using Dapper.Npa.Abstractions.Sorting;
using Dapper.Npa.Core.Enums;
using Npa.Query.Expressions;

public sealed class QueryBuilder<T> where T : class
{
    private readonly List<Expression<Func<T, bool>>> _predicates = new();
    private readonly List<(string column, bool ascending)> _orderBy = new();
    private int? _skip;
    private int? _take;
    private bool _includeDeleted;
    private bool _asSlice;
    private LockMode _lockMode = LockMode.Optimistic;
    private int _lockTimeoutMs;
    private readonly List<string> _includes = new();
    private bool _splitQuery;

    public QueryBuilder<T> Where(Expression<Func<T, bool>> predicate)
    { _predicates.Add(predicate); return this; }

    public QueryBuilder<T> OrderBy(Expression<Func<T, object?>> selector)
    { _orderBy.Add((ExpressionParser.GetMemberName(selector), true)); return this; }

    public QueryBuilder<T> OrderByDescending(Expression<Func<T, object?>> selector)
    { _orderBy.Add((ExpressionParser.GetMemberName(selector), false)); return this; }

    public QueryBuilder<T> ThenBy(Expression<Func<T, object?>> selector)
    { _orderBy.Add((ExpressionParser.GetMemberName(selector), true)); return this; }

    public QueryBuilder<T> ThenByDescending(Expression<Func<T, object?>> selector)
    { _orderBy.Add((ExpressionParser.GetMemberName(selector), false)); return this; }

    public QueryBuilder<T> Skip(int count) { _skip = count; return this; }
    public QueryBuilder<T> Take(int count) { _take = count; return this; }
    public QueryBuilder<T> IncludeDeleted() { _includeDeleted = true; return this; }
    public QueryBuilder<T> AsSlice() { _asSlice = true; return this; }
    public QueryBuilder<T> WithLock(LockMode mode, int timeoutMs = 0)
    {
        if (timeoutMs < 0)
            throw new ArgumentOutOfRangeException(nameof(timeoutMs), "Lock timeout must be greater than or equal to 0 milliseconds.");
        _lockMode = mode;
        _lockTimeoutMs = timeoutMs;
        return this;
    }

    public QueryBuilder<T> Include(string navigationProperty)
    { _includes.Add(navigationProperty); return this; }

    public QueryBuilder<T> ThenInclude(string navigationProperty)
    { _includes.Add(navigationProperty); return this; }

    public QueryBuilder<T> AsSplitQuery()
    { _splitQuery = true; return this; }

    public QueryBuilderState<T> Build() => new()
    {
        Predicates = _predicates,
        OrderBy = _orderBy,
        Skip = _skip,
        Take = _take,
        IncludeDeleted = _includeDeleted,
        AsSlice = _asSlice,
        LockMode = _lockMode,
        LockTimeoutMs = _lockTimeoutMs,
        Includes = _includes,
        SplitQuery = _splitQuery,
    };
}

public sealed class QueryBuilderState<T>
{
    public IReadOnlyList<Expression<Func<T, bool>>> Predicates { get; init; } = [];
    public IReadOnlyList<(string column, bool ascending)> OrderBy { get; init; } = [];
    public int? Skip { get; init; }
    public int? Take { get; init; }
    public bool IncludeDeleted { get; init; }
    public bool AsSlice { get; init; }
    public LockMode LockMode { get; init; }
    public int LockTimeoutMs { get; init; }
    public IReadOnlyList<string> Includes { get; init; } = [];
    public bool SplitQuery { get; init; }
}
