namespace DapperX.Abstractions.Paging;

/// <summary>
/// Content + HasNext flag — no COUNT query.
/// Generator fetches pageSize+1 rows; HasNext = result.Count &gt; pageSize.
/// </summary>
public sealed class Slice<T>
{
    /// <summary>The rows for the requested page (at most <c>pageSize</c> rows).</summary>
    public IReadOnlyList<T> Content { get; }
    /// <summary>Whether at least one more row exists beyond this page.</summary>
    public bool HasNext { get; }

    public Slice(IList<T> rawResults, int pageSize)
    {
        HasNext = rawResults.Count > pageSize;
        Content = HasNext ? rawResults.Take(pageSize).ToList() : (IReadOnlyList<T>)rawResults;
    }
}
