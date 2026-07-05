namespace DapperX.Abstractions.Paging;

/// <summary>A page request: zero-based page number and page size, passed to the paged <c>GetAllAsync</c>/<c>GetAllSliceAsync</c> overloads on <see cref="DapperX.Abstractions.Repositories.IRepository{T, TId}"/>.</summary>
/// <param name="PageNumber">The zero-based page number to fetch.</param>
/// <param name="PageSize">The number of rows per page.</param>
public sealed record Pageable(int PageNumber, int PageSize)
{
    /// <summary>The number of rows to skip to reach <see cref="PageNumber"/>, i.e. <c>PageNumber * PageSize</c>.</summary>
    public int Offset => PageNumber * PageSize;
}
