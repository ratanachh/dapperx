namespace DapperX.Abstractions.Paging;

/// <summary>
/// Content + HasNext flag — no COUNT query.
/// Generator fetches pageSize+1 rows; HasNext = result.Count &gt; pageSize.
/// </summary>
public sealed class Slice<T>
{
    public IReadOnlyList<T> Content { get; }
    public bool HasNext { get; }

    public Slice(IList<T> rawResults, int pageSize)
    {
        HasNext = rawResults.Count > pageSize;
        Content = HasNext ? rawResults.Take(pageSize).ToList() : (IReadOnlyList<T>)rawResults;
    }
}
