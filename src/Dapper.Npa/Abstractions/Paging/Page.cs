namespace Dapper.Npa.Abstractions.Paging;

public sealed class Page<T>
{
    public IReadOnlyList<T> Content { get; }
    public long TotalElements { get; }
    public int PageNumber { get; }
    public int PageSize { get; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling((double)TotalElements / PageSize);

    public Page(IEnumerable<T> content, long totalElements, Pageable pageable)
    {
        Content = content.ToList();
        TotalElements = totalElements;
        PageNumber = pageable.PageNumber;
        PageSize = pageable.PageSize;
    }
}
