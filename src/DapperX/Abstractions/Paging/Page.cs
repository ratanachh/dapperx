namespace DapperX.Abstractions.Paging;

/// <summary>
/// A single page of results plus the total row count across all pages, as returned by
/// <c>GetAllAsync(Pageable, ...)</c> overloads on <see cref="DapperX.Abstractions.Repositories.IRepository{T, TId}"/>. Computing
/// <see cref="TotalElements"/> requires an extra COUNT query; use <see cref="Slice{T}"/> to avoid it.
/// </summary>
public sealed class Page<T>
{
    /// <summary>The rows for the requested page.</summary>
    public IReadOnlyList<T> Content { get; }
    /// <summary>The total number of rows across all pages.</summary>
    public long TotalElements { get; }
    /// <summary>The zero-based page number that was requested.</summary>
    public int PageNumber { get; }
    /// <summary>The page size that was requested.</summary>
    public int PageSize { get; }
    /// <summary>The total number of pages, derived from <see cref="TotalElements"/> and <see cref="PageSize"/>.</summary>
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling((double)TotalElements / PageSize);

    public Page(IEnumerable<T> content, long totalElements, Pageable pageable)
    {
        Content = content.ToList();
        TotalElements = totalElements;
        PageNumber = pageable.PageNumber;
        PageSize = pageable.PageSize;
    }
}
