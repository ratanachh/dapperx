namespace Dapper.Npa.Query.Query;

using System.Linq.Expressions;
using Dapper.Npa.Core.Enums;

/// <summary>Non-generic query builder state for carrying fluent options across entity → DTO projection.</summary>
public sealed class QueryBuilderStateSnapshot
{
    public IReadOnlyList<LambdaExpression> Predicates { get; init; } = [];
    public IReadOnlyList<(string column, bool ascending)> OrderBy { get; init; } = [];
    public int? Skip { get; init; }
    public int? Take { get; init; }
    public bool IncludeDeleted { get; init; }
    public bool AsSlice { get; init; }
    public LockMode LockMode { get; init; } = LockMode.Optimistic;
    public int LockTimeoutMs { get; init; }
    public IReadOnlyList<string> Includes { get; init; } = [];
    public bool SplitQuery { get; init; }

    public static QueryBuilderStateSnapshot From<T>(QueryBuilderState<T> state) where T : class
        => new()
        {
            Predicates = state.Predicates.Cast<LambdaExpression>().ToList(),
            OrderBy = state.OrderBy,
            Skip = state.Skip,
            Take = state.Take,
            IncludeDeleted = state.IncludeDeleted,
            AsSlice = state.AsSlice,
            LockMode = state.LockMode,
            LockTimeoutMs = state.LockTimeoutMs,
            Includes = state.Includes,
            SplitQuery = state.SplitQuery,
        };

    public QueryBuilderStateSnapshot MergeWith(QueryBuilderStateSnapshot current)
        => new()
        {
            Predicates = Predicates.Concat(current.Predicates).ToList(),
            OrderBy = current.OrderBy.Count > 0 ? current.OrderBy : OrderBy,
            Skip = current.Skip ?? Skip,
            Take = current.Take ?? Take,
            IncludeDeleted = IncludeDeleted || current.IncludeDeleted,
            AsSlice = AsSlice || current.AsSlice,
            LockMode = current.LockMode != LockMode.Optimistic ? current.LockMode : LockMode,
            LockTimeoutMs = current.LockTimeoutMs != 0 ? current.LockTimeoutMs : LockTimeoutMs,
            Includes = Includes.Concat(current.Includes).Distinct().ToList(),
            SplitQuery = SplitQuery || current.SplitQuery,
        };
}
